using System.Text.RegularExpressions;

namespace bashGPT.Tools.Testing;

/// <summary>
/// Parses output from `dotnet test` (TRX/console logger).
/// Handles both "Passed: N, Failed: N, Skipped: N" summary lines
/// and individual failure blocks.
/// </summary>
public sealed class DotnetTestOutputParser : ITestOutputParser
{
    // "Total tests: 9" or "Passed: 9" / "Failed:     0" / "Skipped:     0"
    private static readonly Regex PassedRx  = new(@"Passed[!]?\s*-\s*Failed:\s*(\d+),\s*Passed:\s*(\d+),\s*Skipped:\s*(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex SimplePassedRx = new(@"^\s*Passed:\s*(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex SimpleFailedRx = new(@"^\s*Failed:\s*(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    private static readonly Regex SimpleSkippedRx = new(@"^\s*Skipped:\s*(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // xUnit failure: "  Failed SomeTest.Name [12 ms]"
    private static readonly Regex FailedTestRx = new(@"^\s*Failed\s+(.+?)\s+\[", RegexOptions.Multiline);
    // MSTest / NUnit style: "  Failed  TestName"
    private static readonly Regex FailedTestSimpleRx = new(@"^\s*Failed\s+(\S.+)$", RegexOptions.Multiline);

    private const int MaxRawChars = 16_384;

    public TestRunOutput Parse(string rawOutput, long durationMs, bool timedOut)
    {
        var raw = rawOutput.Length > MaxRawChars ? rawOutput[..MaxRawChars] + "\n[truncated]" : rawOutput;

        int passed = 0, failed = 0, skipped = 0;

        // Try combined summary line first: "Passed!  - Failed: 0, Passed: 9, Skipped: 0"
        var m = PassedRx.Match(rawOutput);
        if (m.Success)
        {
            failed  = int.Parse(m.Groups[1].Value);
            passed  = int.Parse(m.Groups[2].Value);
            skipped = int.Parse(m.Groups[3].Value);
        }
        else
        {
            var pm = SimplePassedRx.Match(rawOutput);
            var fm = SimpleFailedRx.Match(rawOutput);
            var sm = SimpleSkippedRx.Match(rawOutput);
            if (pm.Success) passed  = int.Parse(pm.Groups[1].Value);
            if (fm.Success) failed  = int.Parse(fm.Groups[1].Value);
            if (sm.Success) skipped = int.Parse(sm.Groups[1].Value);
        }

        var failures = ExtractFailures(rawOutput);

        return new TestRunOutput(
            Success:    !timedOut && failed == 0,
            Passed:     passed,
            Failed:     failed,
            Skipped:    skipped,
            DurationMs: durationMs,
            TimedOut:   timedOut,
            Failures:   failures,
            RawOutput:  raw);
    }

    private static IReadOnlyList<TestFailure> ExtractFailures(string rawOutput)
    {
        var failures = new List<TestFailure>();
        var matches = FailedTestRx.Matches(rawOutput);
        foreach (Match match in matches)
        {
            var name = match.Groups[1].Value.Trim();
            // Try to grab the error message following the failure header
            var start = match.Index + match.Length;
            var snippet = rawOutput.Length > start + 512 ? rawOutput[start..(start + 512)] : rawOutput[start..];
            var msg = snippet.Split('\n').FirstOrDefault(l => l.Trim().Length > 0)?.Trim() ?? string.Empty;
            failures.Add(new TestFailure(name, msg));
        }
        return failures;
    }
}
