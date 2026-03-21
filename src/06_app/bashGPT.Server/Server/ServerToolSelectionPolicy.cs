namespace bashGPT.Server;

public sealed class ServerToolSelectionPolicy
{
    private static readonly string[] DefaultAllowedToolNames =
    [
        "fetch",
        "filesystem_read",
        "filesystem_search",
        "git_status",
        "git_diff",
        "git_log",
        "git_branch",
    ];

    private readonly HashSet<string> _allowedToolNames;

    public ServerToolSelectionPolicy(IEnumerable<string>? extraAllowedToolNames = null)
    {
        _allowedToolNames = new HashSet<string>(DefaultAllowedToolNames, StringComparer.Ordinal);

        if (extraAllowedToolNames is null)
            return;

        foreach (var name in extraAllowedToolNames)
        {
            var trimmed = name.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                _allowedToolNames.Add(trimmed);
        }
    }

    public static ServerToolSelectionPolicy FromEnvironment()
    {
        var raw = Environment.GetEnvironmentVariable("BASHGPT_SERVER_ALLOWED_TOOLS");
        if (string.IsNullOrWhiteSpace(raw))
            return new ServerToolSelectionPolicy();

        var extraAllowedToolNames = raw
            .Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new ServerToolSelectionPolicy(extraAllowedToolNames);
    }

    public bool IsAllowed(string toolName) => _allowedToolNames.Contains(toolName);

    public IReadOnlyList<string>? FilterRequestedToolNames(IEnumerable<string>? requestedToolNames)
    {
        if (requestedToolNames is null)
            return null;

        return requestedToolNames
            .Where(IsAllowed)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
