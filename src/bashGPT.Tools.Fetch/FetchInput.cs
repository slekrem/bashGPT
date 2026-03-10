namespace BashGPT.Tools.Fetch;

public sealed record FetchInput(
    string Url,
    string Method,
    Dictionary<string, string>? Headers,
    string? Body,
    int TimeoutMs);
