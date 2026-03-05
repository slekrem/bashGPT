using BashGPT.Tools.Abstractions;

namespace BashGPT.Tools.Execution;

public sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool>? tools = null)
    {
        _tools = new Dictionary<string, ITool>(StringComparer.Ordinal);
        if (tools is null) return;
        foreach (var tool in tools)
            Register(tool);
    }

    public IReadOnlyCollection<ITool> Tools => _tools.Values;

    public void Register(ITool tool)
    {
        ArgumentNullException.ThrowIfNull(tool);
        var name = tool.Definition.Name;
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tool name must not be empty.", nameof(tool));
        if (!_tools.TryAdd(name, tool))
            throw new InvalidOperationException($"Tool '{name}' is already registered.");
    }

    public bool TryGet(string name, out ITool? tool) => _tools.TryGetValue(name, out tool);
}
