using BashGPT.Shell;

namespace BashGPT.Server;

/// <summary>
/// Gemeinsamer, thread-sicherer Laufzeit-State des Servers (ExecMode, ForceTools).
/// Wird von SettingsApiHandler geschrieben und von ChatApiHandler gelesen.
/// </summary>
internal sealed class ServerState
{
    // volatile: ExecutionMode ist enum (int-backed), bool – beides erlaubt
    public volatile ExecutionMode ExecMode = ExecutionMode.Ask;
    public volatile bool ForceTools;
}
