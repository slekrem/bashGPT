namespace bashGPT.Tools.Testing;

public interface ITestOutputParser
{
    TestRunOutput Parse(string rawOutput, long durationMs, bool timedOut);
}
