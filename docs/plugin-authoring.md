# Plugin-Authoring Guide

Externe Tools und Agenten können als eigenständige .NET-Assemblies entwickelt und in bashGPT geladen werden, ohne den Host neu zu kompilieren.

## Voraussetzungen

- .NET 9 SDK
- NuGet-Pakete:
  - `bashGPT.Tools` — für externe Tools
  - `bashGPT.Agents` — für externe Agenten (enthält `bashGPT.Tools` transitiv)

```xml
<PackageReference Include="bashGPT.Tools" Version="0.1.0" />
<!-- oder für Agenten: -->
<PackageReference Include="bashGPT.Agents" Version="0.1.0" />
```

## Ein Tool implementieren

Implementiere `ITool` und registriere keine Abhängigkeit auf den Host — das Plugin wird per Reflection geladen.

```csharp
using bashGPT.Tools.Abstractions;

public sealed class HelloTool : ITool
{
    public ToolDefinition Definition => new(
        Name:        "hello",
        Description: "Gibt eine Begrüßung zurück.",
        Parameters:  ToolParameters.Empty
    );

    public Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken ct)
    {
        var result = new ToolResult("Hallo aus dem Plugin!");
        return Task.FromResult(result);
    }
}
```

## Einen Agenten implementieren

Subklasse `AgentBase`, gib ihm eine stabile `Id` und ein oder mehrere `SystemPrompt`-Einträge.

```csharp
using bashGPT.Agents;

public sealed class HelloAgent : AgentBase
{
    public override string Id   => "hello-agent";
    public override string Name => "Hello Agent";

    public override IReadOnlyList<ITool> GetOwnedTools() => [new HelloTool()];

    public override IReadOnlyList<string> SystemPrompt =>
    [
        "Du bist ein freundlicher Assistent, der immer mit 'Hallo!' beginnt."
    ];

    protected override string GetAgentMarkdown() =>
        "# Hello Agent\n\nEin einfacher Demo-Agent.";
}
```

Optionale Overrides:
- `LlmConfig` — eigene Modell- und Sampling-Parameter (`AgentLlmConfig`)
- `EnabledTools` — zusätzliche Registry-Tools nach Name
- `GetSystemPrompt(sessionPath)` — session-abhängige System-Prompts

## Plugin-Layout

Das Plugin-Verzeichnis muss so benannt sein wie die Haupt-Assembly:

```
~/.config/bashgpt/plugins/
  MyPlugin/
    MyPlugin.dll        ← Haupt-Assembly (Name muss Verzeichnis entsprechen)
    SomeDependency.dll  ← private Abhängigkeiten (optional)
```

**Regeln:**
- Öffentliche, nicht-abstrakte `ITool`- und `AgentBase`-Klassen mit parameterlosem Konstruktor werden automatisch instanziiert.
- Built-in-Tools und -Agenten gewinnen bei Namenskonflikten — Duplikate werden nach stderr geloggt und übersprungen.
- Jedes Plugin läuft in einem eigenen `AssemblyLoadContext`. Die SDK-Contracts (`bashGPT.Tools`, `bashGPT.Agents`) fallen auf die Host-Version zurück, um Typidentität zu wahren.

## Installation

```bash
mkdir -p ~/.config/bashgpt/plugins/MyPlugin
cp MyPlugin/bin/Release/net9.0/MyPlugin.dll ~/.config/bashgpt/plugins/MyPlugin/
# optionale Abhängigkeiten ebenfalls kopieren
```

Beim nächsten Start von `bashgpt-server` oder `bashgpt` (CLI) wird das Plugin automatisch geladen.

## Versionierung

Die SDK-Pakete folgen SemVer 2:

| Änderung | Version |
|---|---|
| Breaking change in `ITool`, `AgentBase`, `AgentLlmConfig` | Major-Bump |
| Neue nicht-brechende API | Minor-Bump |
| Bugfixes | Patch-Bump |

> **Hinweis:** Solange die Version `0.x` ist, können Minor-Bumps Breaking Changes enthalten. Fixiere deshalb in Plugin-Projekten auf eine kompatible Version.

## Sicherheitshinweis

Plugins laufen vollständig vertrauenswürdig im gleichen Prozess ohne Sandboxing. Installiere nur Plugins aus vertrauenswürdigen Quellen.
