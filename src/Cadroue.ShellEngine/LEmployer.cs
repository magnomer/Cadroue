using System.Diagnostics;
using System.Text;

namespace Cadroue.ShellEngine;

internal readonly record struct LEmployerResult(int LEmployerExit, string LEmployerError);

internal sealed class LEmployer
{
    private readonly string lEmployerProgramPath;

    internal LEmployer(string lEmployerProgramPath)
    {
        this.lEmployerProgramPath = lEmployerProgramPath;
    }

    internal async Task<LEmployerResult> LEmployerRun(
        string lEmployerArguments,
        CancellationToken lEmployerToken,
        Action<Process> lEmployerAttach,
        Action<string> lEmployerOutputRead,
        Action<string> lEmployerErrorRead)
    {
        var lEmployerStartInfo = new ProcessStartInfo
        {
            FileName = lEmployerProgramPath,
            Arguments = lEmployerArguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var lEmployerProcess = new Process { StartInfo = lEmployerStartInfo };
        lEmployerToken.ThrowIfCancellationRequested();
        lEmployerProcess.Start();
        lEmployerAttach(lEmployerProcess);

        Task<string> lEmployerErrorTask = LEmployerErrorRead(lEmployerProcess, lEmployerToken, lEmployerErrorRead);
        await LEmployerOutputRead(lEmployerProcess, lEmployerToken, lEmployerOutputRead).ConfigureAwait(false);
        await lEmployerProcess.WaitForExitAsync(lEmployerToken).ConfigureAwait(false);
        string lEmployerError = await lEmployerErrorTask.ConfigureAwait(false);
        return new LEmployerResult(lEmployerProcess.ExitCode, lEmployerError);
    }

    private static async Task LEmployerOutputRead(
        Process lEmployerProcess, CancellationToken lEmployerToken, Action<string> lEmployerLine)
    {
        while (await lEmployerProcess.StandardOutput.ReadLineAsync(lEmployerToken).ConfigureAwait(false) is string lEmployerText)
        {
            lEmployerLine(lEmployerText);
        }
    }

    private static async Task<string> LEmployerErrorRead(
        Process lEmployerProcess, CancellationToken lEmployerToken, Action<string> lEmployerLine)
    {
        var lEmployerBuilder = new StringBuilder();
        while (await lEmployerProcess.StandardError.ReadLineAsync(lEmployerToken).ConfigureAwait(false) is string lEmployerText)
        {
            lEmployerBuilder.AppendLine(lEmployerText);
            lEmployerLine(lEmployerText);
        }

        return lEmployerBuilder.ToString();
    }
}
