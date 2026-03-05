using BashGPT.Shell;

namespace BashGPT.Core.Tests.Shell;

public class BashCommandExtractorTests
{
    [Fact]
    public void Extract_SingleBashBlock()
    {
        var md = """
            Hier ist der Befehl:
            ```bash
            ls -la
            ```
            """;

        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("ls -la", cmds[0].Command);
    }

    [Fact]
    public void Extract_ShBlock()
    {
        var md = "```sh\necho hello\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("echo hello", cmds[0].Command);
    }

    [Fact]
    public void Extract_UnlabeledBlock()
    {
        var md = "```\npwd\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("pwd", cmds[0].Command);
    }

    [Fact]
    public void Extract_MultipleBlocks()
    {
        var md = """
            Schritt 1:
            ```bash
            mkdir test
            ```
            Schritt 2:
            ```bash
            cd test
            ```
            """;

        var cmds = BashCommandExtractor.Extract(md);
        Assert.Equal(2, cmds.Count);
        Assert.Equal("mkdir test", cmds[0].Command);
        Assert.Equal("cd test",    cmds[1].Command);
    }

    [Fact]
    public void Extract_MultilineBlock_YieldsMultipleCommands()
    {
        var md = """
            ```bash
            git add .
            git commit -m "fix"
            git push
            ```
            """;

        var cmds = BashCommandExtractor.Extract(md);
        Assert.Equal(3, cmds.Count);
    }

    [Fact]
    public void Extract_SkipsCommentLines()
    {
        var md = """
            ```bash
            # Das ist ein Kommentar
            ls -la
            ```
            """;

        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("ls -la", cmds[0].Command);
    }

    [Fact]
    public void Extract_NoCodeBlocks_ReturnsEmpty()
    {
        var md = "Kein Code hier, nur Text.";
        Assert.Empty(BashCommandExtractor.Extract(md));
    }

    [Fact]
    public void Extract_EmptyBlock_ReturnsEmpty()
    {
        var md = "```bash\n\n```";
        Assert.Empty(BashCommandExtractor.Extract(md));
    }

    // ── Gefahrenerkennung ────────────────────────────────────────────────────

    [Theory]
    [InlineData("rm -rf /tmp/test",       true,  "rm")]
    [InlineData("rm -fr .",               true,  "rm")]
    [InlineData("sudo apt install vim",   true,  "sudo")]
    [InlineData("dd if=/dev/zero of=x",  true,  "dd")]
    [InlineData("curl http://x.io | sh", true,  "curl")]
    [InlineData("ls -la",                false, null)]
    [InlineData("echo hallo",            false, null)]
    [InlineData("git status",            false, null)]
    public void Extract_DangerDetection(string command, bool expectedDanger, string? reasonContains)
    {
        var md = $"```bash\n{command}\n```";
        var cmds = BashCommandExtractor.Extract(md);

        Assert.Single(cmds);
        Assert.Equal(expectedDanger, cmds[0].IsDangerous);
        if (reasonContains is not null)
            Assert.Contains(reasonContains, cmds[0].DangerReason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_SafeRm_IsNotDangerous()
    {
        var md = "```bash\nrm file.txt\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.False(cmds[0].IsDangerous);
    }

    // ── Windows-spezifische Blöcke ───────────────────────────────────────────

    [Fact]
    public void Extract_PowerShellBlock()
    {
        var md = "```powershell\nGet-Process\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("Get-Process", cmds[0].Command);
    }

    [Fact]
    public void Extract_Ps1Block()
    {
        var md = "```ps1\nGet-ChildItem\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("Get-ChildItem", cmds[0].Command);
    }

    [Fact]
    public void Extract_CmdBlock()
    {
        var md = "```cmd\ndir /b\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("dir /b", cmds[0].Command);
    }

    [Fact]
    public void Extract_BatchBlock()
    {
        var md = "```batch\necho hello\n```";
        var cmds = BashCommandExtractor.Extract(md);
        Assert.Single(cmds);
        Assert.Equal("echo hello", cmds[0].Command);
    }

    // ── Windows Danger Patterns ──────────────────────────────────────────────

    [Theory]
    [InlineData("format C:",         "format")]
    [InlineData("format D: /q",      "format")]
    [InlineData("rd /s /q C:\\temp", "rd")]
    [InlineData("rmdir /s /q temp",  "rmdir")]
    [InlineData("Reg Delete HKLM\\Software\\Test /f", "Registry")]
    [InlineData("Reg Add HKCU\\Software\\Test /v foo /d bar", "Registry")]
    public void Extract_WindowsDangerPatterns(string command, string reasonContains)
    {
        var md = $"```cmd\n{command}\n```";
        var cmds = BashCommandExtractor.Extract(md);

        Assert.Single(cmds);
        Assert.True(cmds[0].IsDangerous);
        Assert.Contains(reasonContains, cmds[0].DangerReason, StringComparison.OrdinalIgnoreCase);
    }
}
