namespace bashGPT.Tools.Fetch;

public sealed record FetchOutput(
    int StatusCode,
    Dictionary<string, string> Headers,
    string Body,
    string? RawBody,
    bool BodyTruncated,
    long DurationMs,
    bool TimedOut,
    string? ContentType = null,
    string? ExtractedText = null,
    string? LlmText = null,
    string? Title = null,
    List<FetchHeading>? Headings = null,
    List<string>? Paragraphs = null,
    List<FetchLink>? Links = null);

public sealed record FetchHeading(int Level, string Text);

public sealed record FetchLink(string Text, string Href);
