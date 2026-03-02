using System.Net;
using BashGPT.Shell;

namespace BashGPT.Server;

internal sealed class ContextApiHandler
{
    public async Task HandleAsync(HttpListenerResponse response, CancellationToken ct)
    {
        var shellCtx = await new ShellContextCollector().CollectAsync(includeDirectoryListing: false);
        await ApiResponse.WriteJsonAsync(response, new
        {
            user = Environment.UserName,
            host = Environment.MachineName,
            cwd  = shellCtx.WorkingDirectory,
            os   = shellCtx.OperatingSystem,
            shell = shellCtx.Shell,
            git  = shellCtx.Git == null ? null : (object)new
            {
                branch           = shellCtx.Git.Branch,
                lastCommit       = shellCtx.Git.LastCommit,
                changedFilesCount = shellCtx.Git.ChangedFiles.Count,
            },
        });
    }
}
