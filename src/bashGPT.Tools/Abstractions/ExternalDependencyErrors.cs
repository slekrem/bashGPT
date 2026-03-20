using System.ComponentModel;

namespace BashGPT.Tools.Abstractions;

/// <summary>
/// Describes a missing external executable required by a tool.
/// </summary>
public sealed class MissingExecutableException : Exception
{
    public MissingExecutableException(string executable, string nextStep, Exception innerException)
        : base(CreateMessage(executable, nextStep), innerException)
    {
        Executable = executable;
        NextStep = nextStep;
    }

    public string Executable { get; }

    public string NextStep { get; }

    private static string CreateMessage(string executable, string nextStep) =>
        $"missing_dependency: Required executable '{executable}' was not found on PATH. Next step: {nextStep}";
}

public static class ExternalDependencyErrors
{
    /// <summary>
    /// Converts common process-launch failures into a stable missing-dependency exception.
    /// </summary>
    public static MissingExecutableException? TryCreateMissingExecutableException(
        string executable,
        string nextStep,
        Exception exception)
    {
        if (exception is FileNotFoundException)
            return new MissingExecutableException(executable, nextStep, exception);

        if (exception is Win32Exception win32Exception && LooksLikeMissingExecutable(win32Exception))
            return new MissingExecutableException(executable, nextStep, exception);

        return null;
    }

    private static bool LooksLikeMissingExecutable(Win32Exception exception)
    {
        if (exception.NativeErrorCode == 2)
            return true;

        var message = exception.Message.ToLowerInvariant();
        return message.Contains("no such file or directory", StringComparison.Ordinal)
            || message.Contains("cannot find the file", StringComparison.Ordinal);
    }
}
