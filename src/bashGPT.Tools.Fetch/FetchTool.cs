using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using bashGPT.Tools.Abstractions;

namespace bashGPT.Tools.Fetch;

public sealed class FetchTool : ITool
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly IFetchPolicy _policy;
    private readonly Action<FetchInput, FetchOutput>? _onExecuted;
    private readonly HttpClient _httpClient;

    public FetchTool(IFetchPolicy? policy = null, Action<FetchInput, FetchOutput>? onExecuted = null, HttpClient? httpClient = null)
    {
        _policy = policy ?? new DefaultFetchPolicy();
        _onExecuted = onExecuted;
        _httpClient = httpClient ?? new HttpClient();
    }

    public ToolDefinition Definition { get; } = new(
        Name: "fetch",
        Description: "Executes an HTTP request and returns LLM-optimized text content.",
        Parameters:
        [
            new ToolParameter("url", "string", "The URL to request.", Required: true),
            new ToolParameter("method", "string", "HTTP method (GET, POST, PUT, PATCH, DELETE, HEAD). Default: GET.", Required: false),
            new ToolParameter("headers", "object", "HTTP request headers as key-value pairs.", Required: false),
            new ToolParameter("body", "string", "Request body (for POST, PUT, PATCH).", Required: false),
            new ToolParameter("timeoutMs", "integer", "Timeout in milliseconds (default: 10000).", Required: false),
        ]);

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        FetchInput input;
        try
        {
            input = ParseInput(call.ArgumentsJson);
        }
        catch (JsonException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments [invalid_json]: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Invalid arguments: {ex.Message}");
        }

        if (!_policy.Allow(input))
            return new ToolResult(Success: false, Content: "Request blocked by policy.");

        FetchOutput output;
        try
        {
            output = await RunAsync(input, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Request failed: {ex.Message}");
        }

        _onExecuted?.Invoke(input, output);

        var success = !output.TimedOut && output.StatusCode >= 200 && output.StatusCode < 300;
        return new ToolResult(Success: success, Content: BuildLlmResultText(output));
    }

    private static FetchInput ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("url", out var urlEl))
            throw new ArgumentException("missing_required_field: 'url' is required. Example: {\"url\":\"https://example.com\",\"method\":\"GET\"}");
        if (urlEl.ValueKind != JsonValueKind.String)
            throw new ArgumentException("invalid_type: 'url' must be a string.");
        var url = urlEl.GetString();
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("invalid_value: 'url' must not be empty.");

        string method = ReadOptionalString(root, "method") ?? "GET";
        if (string.IsNullOrWhiteSpace(method))
            method = "GET";
        string? body = ReadOptionalString(root, "body");
        int timeoutMs = ReadOptionalInt(root, "timeoutMs") ?? 10_000;
        if (timeoutMs <= 0)
            throw new ArgumentException("invalid_value: 'timeoutMs' must be greater than 0.");

        Dictionary<string, string>? headers = null;
        if (root.TryGetProperty("headers", out var headersEl))
        {
            if (headersEl.ValueKind is not (JsonValueKind.Object or JsonValueKind.Null))
                throw new ArgumentException("invalid_type: 'headers' must be an object.");

            if (headersEl.ValueKind == JsonValueKind.Object)
            {
                headers = [];
                foreach (var prop in headersEl.EnumerateObject())
                {
                    if (prop.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Null))
                        throw new ArgumentException($"invalid_type: header '{prop.Name}' must be a string.");
                    headers[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }
        }

        return new FetchInput(url, method, headers, body, timeoutMs);
    }

    private static string? ReadOptionalString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.String => valueEl.GetString(),
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be a string."),
        };
    }

    private static int? ReadOptionalInt(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var valueEl)) return null;
        return valueEl.ValueKind switch
        {
            JsonValueKind.Number when valueEl.TryGetInt32(out var i) => i,
            JsonValueKind.Null => null,
            _ => throw new ArgumentException($"invalid_type: '{name}' must be an integer."),
        };
    }

    private async Task<FetchOutput> RunAsync(FetchInput input, CancellationToken externalCt)
    {
        using var timeoutCts = new CancellationTokenSource(input.TimeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCt, timeoutCts.Token);

        var request = new HttpRequestMessage(new HttpMethod(input.Method.ToUpperInvariant()), input.Url);

        if (input.Headers is not null)
        {
            foreach (var (key, value) in input.Headers)
                request.Headers.TryAddWithoutValidation(key, value);
        }

        if (input.Body is not null)
            request.Content = new StringContent(input.Body, Encoding.UTF8);

        var sw = Stopwatch.StartNew();
        bool timedOut = false;
        HttpResponseMessage? response = null;

        try
        {
            response = await _httpClient.SendAsync(request, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutCts.IsCancellationRequested;
            if (!timedOut && externalCt.IsCancellationRequested)
                throw;
            sw.Stop();
            return new FetchOutput(StatusCode: 0, Headers: [], Body: string.Empty, RawBody: null, BodyTruncated: false, DurationMs: sw.ElapsedMilliseconds, TimedOut: timedOut);
        }

        var responseBody = await ReadAsync(response.Content, linkedCts.Token);
        sw.Stop();

        // ReadAsync schluckt OperationCanceledException – Timeout-Zustand nachholen
        if (timeoutCts.IsCancellationRequested)
            timedOut = true;

        var contentType = response.Content.Headers.ContentType?.ToString();
        var html = TryExtractHtml(contentType, responseBody);
        var optimizedBody = html?.LlmText;
        var outputBody = !string.IsNullOrWhiteSpace(optimizedBody) ? optimizedBody : responseBody;
        var rawBody = !string.IsNullOrWhiteSpace(optimizedBody) ? responseBody : null;

        var responseHeaders = new Dictionary<string, string>();
        foreach (var header in response.Headers)
            responseHeaders[header.Key] = string.Join(", ", header.Value);
        foreach (var header in response.Content.Headers)
            responseHeaders[header.Key] = string.Join(", ", header.Value);

        return new FetchOutput(
            StatusCode: (int)response.StatusCode,
            Headers: responseHeaders,
            Body: outputBody,
            RawBody: rawBody,
            BodyTruncated: false,
            DurationMs: sw.ElapsedMilliseconds,
            TimedOut: timedOut,
            ContentType: contentType,
            ExtractedText: html?.ExtractedText,
            LlmText: html?.LlmText,
            Title: html?.Title,
            Headings: html?.Headings,
            Paragraphs: html?.Paragraphs,
            Links: html?.Links);
    }

    private static async Task<string> ReadAsync(HttpContent content, CancellationToken ct)
    {
        try
        {
            return await content.ReadAsStringAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }

    private static HtmlExtraction? TryExtractHtml(string? contentType, string body)
    {
        if (string.IsNullOrWhiteSpace(body) || !IsLikelyHtml(contentType, body))
            return null;

        try
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(body);

            RemoveElements(document, "script, style, noscript, template, nav, footer, aside");

            var root = document.Body ?? document.DocumentElement;
            if (root is null)
                return null;
            var contentRoot = SelectContentRoot(document) ?? root;

            var extractedText = NormalizeText(root.TextContent);
            var title = NormalizeText(document.Title);
            var llmText = BuildLlmText(document, contentRoot, title);
            var headings = document.QuerySelectorAll("h1, h2, h3, h4, h5, h6")
                .Select(ToHeading)
                .Where(static h => h is not null)
                .Cast<FetchHeading>()
                .ToList();

            var paragraphs = document.QuerySelectorAll("p")
                .Select(static p => NormalizeText(p.TextContent))
                .Where(static text => !string.IsNullOrWhiteSpace(text))
                .ToList();

            var links = document.QuerySelectorAll("a[href]")
                .Select(static a => ToLink(a))
                .Where(static l => l is not null)
                .Cast<FetchLink>()
                .ToList();

            if (string.IsNullOrWhiteSpace(extractedText) &&
                string.IsNullOrWhiteSpace(title) &&
                headings.Count == 0 &&
                paragraphs.Count == 0 &&
                links.Count == 0)
            {
                return null;
            }

            return new HtmlExtraction(
                ExtractedText: string.IsNullOrWhiteSpace(extractedText) ? null : extractedText,
                LlmText: string.IsNullOrWhiteSpace(llmText) ? null : llmText,
                Title: string.IsNullOrWhiteSpace(title) ? null : title,
                Headings: headings,
                Paragraphs: paragraphs,
                Links: links);
        }
        catch
        {
            return null;
        }
    }

    private static void RemoveElements(IDocument document, string selector)
    {
        foreach (var node in document.QuerySelectorAll(selector).ToArray())
            node.Remove();
    }

    private static bool IsLikelyHtml(string? contentType, string body)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            if (contentType.Contains("text/html", StringComparison.OrdinalIgnoreCase) ||
                contentType.Contains("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return body.Contains("<html", StringComparison.OrdinalIgnoreCase) ||
               body.Contains("<body", StringComparison.OrdinalIgnoreCase);
    }

    private static IElement? SelectContentRoot(IDocument document)
    {
        var candidates = new[]
        {
            "main",
            "article",
            "[role='main']",
            "#content",
            ".content",
            "#main",
            ".main"
        };

        foreach (var selector in candidates)
        {
            var candidate = document.QuerySelector(selector);
            if (candidate is not null)
                return candidate;
        }

        return document.Body;
    }

    private static string BuildLlmText(IDocument document, IElement contentRoot, string title)
    {
        var lines = new List<string>();

        if (!string.IsNullOrWhiteSpace(title))
            lines.Add($"Title: {title}");

        var blocks = contentRoot.QuerySelectorAll("h1, h2, h3, h4, h5, h6, p, li, blockquote, pre");
        foreach (var block in blocks)
        {
            var line = FormatBlock(block);
            if (!string.IsNullOrWhiteSpace(line))
                lines.Add(line);
        }

        var links = document.QuerySelectorAll("a[href]")
            .Select(static a => ToLink(a))
            .Where(static l => l is not null)
            .Cast<FetchLink>()
            .DistinctBy(static l => l.Href)
            .Take(30)
            .Select(static l => string.IsNullOrWhiteSpace(l.Text) ? l.Href : $"{l.Text}: {l.Href}")
            .ToList();

        var deduped = lines
            .Select(static line => line.Trim())
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .Distinct()
            .ToList();

        if (links.Count > 0)
        {
            deduped.Add("Links:");
            deduped.AddRange(links.Select(static l => $"- {l}"));
        }

        return string.Join("\n", deduped);
    }

    private static string? FormatBlock(IElement block)
    {
        var text = NormalizeText(block.TextContent);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        return block.LocalName switch
        {
            "h1" => $"# {text}",
            "h2" => $"## {text}",
            "h3" => $"### {text}",
            "h4" => $"#### {text}",
            "h5" => $"##### {text}",
            "h6" => $"###### {text}",
            "li" => $"- {text}",
            "blockquote" => $"> {text}",
            "pre" => $"Code: {text}",
            _ => text
        };
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return WhitespaceRegex.Replace(value, " ").Trim();
    }

    private static FetchHeading? ToHeading(IElement element)
    {
        if (element.LocalName.Length != 2 || element.LocalName[0] != 'h' || !char.IsDigit(element.LocalName[1]))
            return null;

        var text = NormalizeText(element.TextContent);
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var level = element.LocalName[1] - '0';
        return new FetchHeading(level, text);
    }

    private static FetchLink? ToLink(IElement element)
    {
        var href = element.GetAttribute("href");
        if (string.IsNullOrWhiteSpace(href))
            return null;

        var text = NormalizeText(element.TextContent);
        return new FetchLink(text, href);
    }

    private sealed record HtmlExtraction(
        string? ExtractedText,
        string? LlmText,
        string? Title,
        List<FetchHeading> Headings,
        List<string> Paragraphs,
        List<FetchLink> Links);

    private static string BuildLlmResultText(FetchOutput output)
    {
        if (output.TimedOut)
            return "Request timed out.";

        if (!string.IsNullOrWhiteSpace(output.Body))
            return output.Body;

        if (output.StatusCode > 0)
            return $"HTTP {output.StatusCode}";

        return string.Empty;
    }
}
