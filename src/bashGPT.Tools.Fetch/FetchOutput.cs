namespace BashGPT.Tools.Fetch;

public sealed record FetchOutput(
    int StatusCode,
    Dictionary<string, string> Headers,
    string Body,
    long DurationMs,
    bool TimedOut);
