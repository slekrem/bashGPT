using BashGPT.Shell;

namespace BashGPT.Tests.Shell;

public class ShellContextCollectorTests
{
    private readonly ShellContextCollector _sut = new();

    [Fact]
    public async Task CollectAsync_PopulatesWorkingDirectory()
    {
        var ctx = await _sut.CollectAsync();
        Assert.Equal(Directory.GetCurrentDirectory(), ctx.WorkingDirectory);
    }

    [Fact]
    public async Task CollectAsync_PopulatesOperatingSystem()
    {
        var ctx = await _sut.CollectAsync();
        Assert.False(string.IsNullOrWhiteSpace(ctx.OperatingSystem));
    }

    [Fact]
    public async Task CollectAsync_PopulatesShell()
    {
        var ctx = await _sut.CollectAsync();
        Assert.False(string.IsNullOrWhiteSpace(ctx.Shell));
    }

    [Fact]
    public async Task CollectAsync_WithoutDirectoryListing_IsEmpty()
    {
        var ctx = await _sut.CollectAsync(includeDirectoryListing: false);
        Assert.Empty(ctx.DirectoryEntries);
    }

    [Fact]
    public async Task CollectAsync_WithDirectoryListing_IsNotEmpty()
    {
        // Temp-Verzeichnis mit bekanntem Inhalt
        var tmp = Path.Combine(Path.GetTempPath(), "bashgpt-ctx-test-" + Guid.NewGuid());
        Directory.CreateDirectory(tmp);
        File.WriteAllText(Path.Combine(tmp, "test.txt"), "");
        var subDir = Path.Combine(tmp, "subdir");
        Directory.CreateDirectory(subDir);

        try
        {
            Directory.SetCurrentDirectory(tmp);
            var ctx = await _sut.CollectAsync(includeDirectoryListing: true);

            Assert.Contains(ctx.DirectoryEntries, e => e == "test.txt");
            Assert.Contains(ctx.DirectoryEntries, e => e == "subdir/");
        }
        finally
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public async Task CollectAsync_Environment_ContainsOnlyAllowedKeys()
    {
        var ctx = await _sut.CollectAsync();
        var forbidden = new[] { "HOME", "PASSWORD", "SECRET", "TOKEN", "KEY", "API" };

        foreach (var key in ctx.Environment.Keys)
        {
            // Sicherstellen dass keine offensichtlichen Secrets drin sind
            Assert.DoesNotContain("PASSWORD", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SECRET",   key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("TOKEN",    key, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CollectAsync_InGitRepo_HasGitContext()
    {
        // Der Test-Runner läuft im bashGPT-Repo → Git sollte erkannt werden
        var ctx = await _sut.CollectAsync();

        // Nur prüfen wenn tatsächlich in einem Git-Repo
        if (ctx.Git is not null)
        {
            Assert.False(string.IsNullOrWhiteSpace(ctx.Git.Branch));
            Assert.NotNull(ctx.Git.ChangedFiles);
        }
    }

    [Fact]
    public async Task CollectAsync_OutsideGitRepo_GitIsNull()
    {
        var tmp = Path.Combine(Path.GetTempPath(), "bashgpt-nogit-" + Guid.NewGuid());
        Directory.CreateDirectory(tmp);

        try
        {
            Directory.SetCurrentDirectory(tmp);
            var ctx = await _sut.CollectAsync();
            Assert.Null(ctx.Git);
        }
        finally
        {
            Directory.SetCurrentDirectory(Path.GetTempPath());
            Directory.Delete(tmp, recursive: true);
        }
    }

    [Fact]
    public async Task BuildSystemPrompt_ContainsAllSections()
    {
        var ctx = await _sut.CollectAsync();
        var prompt = _sut.BuildSystemPrompt(ctx);

        Assert.Contains("Shell-Assistent",    prompt);
        Assert.Contains("Verzeichnis",        prompt);
        Assert.Contains("OS",                 prompt);
        Assert.Contains("Shell",              prompt);
        Assert.Contains("bash-Tool",          prompt);
        Assert.Contains("Regeln",             prompt);
    }

    [Fact]
    public async Task BuildSystemPrompt_ContainsWorkingDirectory()
    {
        var ctx = await _sut.CollectAsync();
        var prompt = _sut.BuildSystemPrompt(ctx);

        Assert.Contains(ctx.WorkingDirectory, prompt);
    }

    [Fact]
    public void BuildSystemPrompt_WithGit_ContainsBranch()
    {
        var git = new GitContext("main", "abc1234 Initial commit", ["M src/foo.cs"]);
        var ctx = new ShellContext(
            WorkingDirectory: "/home/user/project",
            OperatingSystem:  "macOS (Arm64)",
            Shell:            "test-shell",
            Git:              git,
            DirectoryEntries: [],
            Environment:      new Dictionary<string, string>());

        var prompt = _sut.BuildSystemPrompt(ctx);

        Assert.Contains("main",                  prompt);
        Assert.Contains("abc1234 Initial commit", prompt);
        Assert.Contains("src/foo.cs",            prompt);
    }

    [Fact]
    public void BuildSystemPrompt_WithoutGit_ShowsNoRepo()
    {
        var ctx = new ShellContext(
            WorkingDirectory: "/tmp",
            OperatingSystem:  "Linux",
            Shell:            "test-shell",
            Git:              null,
            DirectoryEntries: [],
            Environment:      new Dictionary<string, string>());

        var prompt = _sut.BuildSystemPrompt(ctx);
        Assert.Contains("Kein Git-Repository", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_LimitsChangedFilesTo10()
    {
        var manyFiles = Enumerable.Range(1, 15).Select(i => $"M file{i}.cs").ToList();
        var git = new GitContext("feature", null, manyFiles);
        var ctx = new ShellContext("/tmp", "Linux", "test-shell", git, [], new Dictionary<string, string>());

        var prompt = _sut.BuildSystemPrompt(ctx);
        Assert.Contains("5 weitere", prompt);
    }
}
