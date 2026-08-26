using System.Diagnostics;
using System.Text;

using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal readonly record struct LEmployerResult(int LEmployerExit, string LEmployerError);

internal sealed class LEmployer
{
    private const int LEmployerErrorLimit = 256 * 1024;
    private readonly string lEmployerProgramPath;
    private readonly string lEmployerArgumentPrefix;

    internal LEmployer(string lEmployerProgramPath, string lEmployerArgumentPrefix = "")
    {
        this.lEmployerProgramPath = lEmployerProgramPath;
        this.lEmployerArgumentPrefix = lEmployerArgumentPrefix;
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
            Arguments = string.IsNullOrWhiteSpace(lEmployerArgumentPrefix)
                ? lEmployerArguments
                : $"{lEmployerArgumentPrefix} {lEmployerArguments}",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var lEmployerProcess = new Process { StartInfo = lEmployerStartInfo };
        lEmployerToken.ThrowIfCancellationRequested();
        lEmployerProcess.Start();
        LCustody.LCustodyAttach(lEmployerProcess);
        LEmployerPrioritySet(lEmployerProcess);
        lEmployerAttach(lEmployerProcess);

        Task<string> lEmployerErrorTask = LEmployerErrorRead(lEmployerProcess, lEmployerToken, lEmployerErrorRead);
        await LEmployerOutputRead(lEmployerProcess, lEmployerToken, lEmployerOutputRead).ConfigureAwait(false);
        await lEmployerProcess.WaitForExitAsync(lEmployerToken).ConfigureAwait(false);
        string lEmployerError = await lEmployerErrorTask.ConfigureAwait(false);
        return new LEmployerResult(lEmployerProcess.ExitCode, lEmployerError);
    }

    private static void LEmployerPrioritySet(Process lEmployerProcess)
    {
        try
        {
            lEmployerProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception lEmployerException)
            when (lEmployerException is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
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
        bool lEmployerTruncated = false;
        while (await lEmployerProcess.StandardError.ReadLineAsync(lEmployerToken).ConfigureAwait(false) is string lEmployerText)
        {
            lEmployerBuilder.AppendLine(lEmployerText);
            if (lEmployerBuilder.Length > LEmployerErrorLimit * 2)
            {
                lEmployerBuilder.Remove(0, lEmployerBuilder.Length - LEmployerErrorLimit);
                lEmployerTruncated = true;
            }

            lEmployerLine(lEmployerText);
        }

        if (lEmployerBuilder.Length > LEmployerErrorLimit)
        {
            lEmployerBuilder.Remove(0, lEmployerBuilder.Length - LEmployerErrorLimit);
            lEmployerTruncated = true;
        }

        return lEmployerTruncated
            ? "[Earlier FFmpeg stderr was truncated.]\n" + lEmployerBuilder
            : lEmployerBuilder.ToString();
    }
}
