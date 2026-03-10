namespace BashGPT.Tools.Testing;

/// <summary>
/// Fallback parser for runners without structured output support (e.g. npm, pytest).
/// Returns raw output without parsing pass/fail counts.
/// </summary>
public sealed class RawOutputParser : ITestOutputParser
{
    private const int MaxRawChars = 16_384;

    public TestRunOutput Parse(string rawOutput, long durationMs, bool timedOut)
    {
        var raw = rawOutput.Length > MaxRawChars ? rawOutput[..MaxRawChars] + "\n[truncated]" : rawOutput;
        return new TestRunOutput(
            Success:    !timedOut,
            Passed:     0,
            Failed:     0,
            Skipped:    0,
            DurationMs: durationMs,
            TimedOut:   timedOut,
            Failures:   [],
            RawOutput:  raw);
    }
}
