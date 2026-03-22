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

    public static ServerToolSelectionPolicy FromEnvironment(IEnumerable<string>? additionalAllowedToolNames = null)
    {
        var raw = Environment.GetEnvironmentVariable("BASHGPT_SERVER_ALLOWED_TOOLS");

        var fromEnv = string.IsNullOrWhiteSpace(raw)
            ? []
            : raw.Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var combined = additionalAllowedToolNames is null
            ? fromEnv
            : fromEnv.Concat(additionalAllowedToolNames);

        return new ServerToolSelectionPolicy(combined);
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
