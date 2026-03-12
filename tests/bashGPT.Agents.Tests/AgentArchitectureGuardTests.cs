using System.Reflection;
using BashGPT.Agents;

namespace BashGPT.Agents.Tests;

public class AgentArchitectureGuardTests
{
    [Fact]
    public void AgentBootstrap_RemainsInfrastructureOnly()
    {
        var type = typeof(AgentBootstrap);

        var forbiddenFields = type
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => typeof(AgentRecord).IsAssignableFrom(f.FieldType)
                     || typeof(IEnumerable<AgentRecord>).IsAssignableFrom(f.FieldType))
            .Select(f => f.Name)
            .ToList();

        var forbiddenMethods = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.Contains("Seed", StringComparison.OrdinalIgnoreCase)
                     || typeof(AgentRecord).IsAssignableFrom(m.ReturnType)
                     || typeof(IEnumerable<AgentRecord>).IsAssignableFrom(m.ReturnType))
            .Select(m => m.Name)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        Assert.Empty(forbiddenFields);
        Assert.Empty(forbiddenMethods);
    }
}
