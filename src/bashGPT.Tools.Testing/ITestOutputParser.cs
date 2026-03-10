namespace BashGPT.Tools.Testing;

public interface ITestOutputParser
{
    TestRunOutput Parse(string rawOutput, long durationMs, bool timedOut);
}
