namespace bashGPT.Core.Models.Storage;

/// <summary>
/// Records a single POST request stored under
/// sessions/&lt;id&gt;/requests/&lt;timestamp&gt;.json.
/// Keeps request and response data separately.
/// </summary>
public sealed class SessionRequestRecord
{
    public string Timestamp { get; set; } = string.Empty;
    public SessionRequestData Request { get; set; } = new();
    public SessionResponseData Response { get; set; } = new();
}
