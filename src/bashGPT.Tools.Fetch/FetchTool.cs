using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Fetch;

public sealed class FetchTool : ITool
{
    private const int MaxBodyChars = 32_768;

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
        Description: "Executes an HTTP request and returns status code, headers, body, duration and timeout status.",
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
        catch (Exception ex)
        {
            return new ToolResult(Success: false, Content: $"Request failed: {ex.Message}");
        }

        _onExecuted?.Invoke(input, output);

        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return new ToolResult(Success: !output.TimedOut && output.StatusCode >= 200 && output.StatusCode < 300, Content: json);
    }

    private static FetchInput ParseInput(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var url = root.GetProperty("url").GetString()
            ?? throw new ArgumentException("url must not be null");

        string method = root.TryGetProperty("method", out var methodEl) ? methodEl.GetString() ?? "GET" : "GET";
        string? body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() : null;
        int timeoutMs = root.TryGetProperty("timeoutMs", out var toEl) ? toEl.GetInt32() : 10_000;

        Dictionary<string, string>? headers = null;
        if (root.TryGetProperty("headers", out var headersEl) && headersEl.ValueKind == JsonValueKind.Object)
        {
            headers = [];
            foreach (var prop in headersEl.EnumerateObject())
                headers[prop.Name] = prop.Value.GetString() ?? string.Empty;
        }

        return new FetchInput(url, method, headers, body, timeoutMs);
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
            sw.Stop();
            return new FetchOutput(StatusCode: 0, Headers: [], Body: string.Empty, DurationMs: sw.ElapsedMilliseconds, TimedOut: timedOut);
        }

        var responseBody = await ReadLimitedAsync(response.Content, MaxBodyChars, linkedCts.Token);
        sw.Stop();

        var responseHeaders = new Dictionary<string, string>();
        foreach (var header in response.Headers)
            responseHeaders[header.Key] = string.Join(", ", header.Value);
        foreach (var header in response.Content.Headers)
            responseHeaders[header.Key] = string.Join(", ", header.Value);

        return new FetchOutput(
            StatusCode: (int)response.StatusCode,
            Headers: responseHeaders,
            Body: responseBody,
            DurationMs: sw.ElapsedMilliseconds,
            TimedOut: false);
    }

    private static async Task<string> ReadLimitedAsync(HttpContent content, int maxChars, CancellationToken ct)
    {
        try
        {
            var body = await content.ReadAsStringAsync(ct);
            return body.Length > maxChars ? body[..maxChars] : body;
        }
        catch (OperationCanceledException)
        {
            return string.Empty;
        }
    }
}
