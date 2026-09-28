using System.Diagnostics;
using System.Text;

using Cadroue.Core;

namespace Cadroue.Media;

public readonly record struct LEmployerResult(int LEmployerExit, string LEmployerError, bool LEmployerStalled = false);

public sealed class LEmployer
{
    private const int LEmployerErrorLimit = 256 * 1024;
    private static readonly TimeSpan lEmployerExitWait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan lEmployerPulseWait = TimeSpan.FromMilliseconds(500);
    private readonly string lEmployerProgramPath;
    private readonly string lEmployerArgumentPrefix;

    public LEmployer(string lEmployerProgramPath, string lEmployerArgumentPrefix = "")
    {
        this.lEmployerProgramPath = lEmployerProgramPath;
        this.lEmployerArgumentPrefix = lEmployerArgumentPrefix;
    }

    public LCustodyFamily? LEmployerFamily { get; init; }

    public TimeSpan? LEmployerIdleLimit { get; init; }

    public Task<LEmployerResult> LEmployerRun(
        string lEmployerArguments,
        CancellationToken lEmployerToken,
        Action<Process> lEmployerAttach,
        Action<string> lEmployerOutputRead,
        Action<string> lEmployerErrorRead) =>
        LEmployerProcessRun(
            LEmployerInfoCreate(lEmployerArguments, null),
            lEmployerToken,
            lEmployerAttach,
            (lEmployerProcess, lEmployerPulse, lEmployerCancel) =>
                LEmployerLineRead(lEmployerProcess, lEmployerPulse, lEmployerCancel, lEmployerOutputRead),
            lEmployerErrorRead);

    public Task<LEmployerResult> LEmployerRun(
        IReadOnlyList<string> lEmployerArguments,
        CancellationToken lEmployerToken,
        Action<string> lEmployerOutputRead) =>
        LEmployerProcessRun(
            LEmployerInfoCreate(null, lEmployerArguments),
            lEmployerToken,
            null,
            (lEmployerProcess, lEmployerPulse, lEmployerCancel) =>
                LEmployerLineRead(lEmployerProcess, lEmployerPulse, lEmployerCancel, lEmployerOutputRead),
            null);

    public Task<LEmployerResult> LEmployerStreamRun(
        IReadOnlyList<string> lEmployerArguments,
        CancellationToken lEmployerToken,
        Func<Stream, CancellationToken, Task> lEmployerOutputRead) =>
        LEmployerProcessRun(
            LEmployerInfoCreate(null, lEmployerArguments),
            lEmployerToken,
            null,
            async (lEmployerProcess, _, lEmployerCancel) =>
            {
                Stream lEmployerStream = lEmployerProcess.StandardOutput.BaseStream;
                await lEmployerOutputRead(lEmployerStream, lEmployerCancel).ConfigureAwait(false);
                await lEmployerStream.CopyToAsync(Stream.Null, lEmployerCancel).ConfigureAwait(false);
            },
            null);

    internal ProcessStartInfo LEmployerInfoCreate(string? lEmployerArguments, IReadOnlyList<string>? lEmployerList)
    {
        var lEmployerInfo = new ProcessStartInfo
        {
            FileName = lEmployerProgramPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        if (lEmployerList is null)
        {
            lEmployerInfo.Arguments = string.IsNullOrWhiteSpace(lEmployerArgumentPrefix)
                ? lEmployerArguments
                : $"{lEmployerArgumentPrefix} {lEmployerArguments}";
            return lEmployerInfo;
        }

        if (!string.IsNullOrWhiteSpace(lEmployerArgumentPrefix))
        {
            throw new InvalidOperationException("An argument list cannot be combined with an argument prefix.");
        }

        foreach (string lEmployerArgument in lEmployerList)
        {
            lEmployerInfo.ArgumentList.Add(lEmployerArgument);
        }

        return lEmployerInfo;
    }

    private async Task<LEmployerResult> LEmployerProcessRun(
        ProcessStartInfo lEmployerInfo,
        CancellationToken lEmployerToken,
        Action<Process>? lEmployerAttach,
        Func<Process, Action, CancellationToken, Task> lEmployerOutputRead,
        Action<string>? lEmployerErrorRead)
    {
        lEmployerToken.ThrowIfCancellationRequested();
        using var lEmployerProcess = new Process { StartInfo = lEmployerInfo };
        lEmployerProcess.Start();
        LCustody.LCustodyAttach(lEmployerProcess);
        LEmployerPriorityApply(lEmployerProcess);
        lEmployerAttach?.Invoke(lEmployerProcess);

        long lEmployerPulse = Environment.TickCount64;
        void lEmployerPulseSet() => Volatile.Write(ref lEmployerPulse, Environment.TickCount64);
        long lEmployerPulseRead() => Volatile.Read(ref lEmployerPulse);

        using var lEmployerCancel = CancellationTokenSource.CreateLinkedTokenSource(lEmployerToken);
        Task lEmployerWatch = LEmployerIdleLimit is TimeSpan lEmployerLimit
            ? LEmployerIdleRun(lEmployerPulseRead, lEmployerLimit, lEmployerCancel)
            : Task.CompletedTask;
        Task<string> lEmployerErrorTask = LEmployerErrorRead(lEmployerProcess, lEmployerPulseSet, lEmployerErrorRead);
        try
        {
            await lEmployerOutputRead(lEmployerProcess, lEmployerPulseSet, lEmployerCancel.Token).ConfigureAwait(false);
            await lEmployerProcess.WaitForExitAsync(lEmployerCancel.Token).ConfigureAwait(false);
            string lEmployerError = await lEmployerErrorTask.ConfigureAwait(false);
            return new LEmployerResult(lEmployerProcess.ExitCode, lEmployerError);
        }
        catch (OperationCanceledException) when (lEmployerCancel.IsCancellationRequested)
        {
            if (!await LEmployerProcessInterrupt(lEmployerProcess).ConfigureAwait(false))
            {
                throw new InvalidOperationException(
                    $"the process '{lEmployerProgramPath}' did not terminate within " +
                    $"{lEmployerExitWait.TotalSeconds:0} s of cancellation");
            }

            if (lEmployerToken.IsCancellationRequested)
            {
                throw;
            }

            return new LEmployerResult(-1, await lEmployerErrorTask.ConfigureAwait(false), true);
        }
        catch (Exception)
        {
            await LEmployerProcessInterrupt(lEmployerProcess).ConfigureAwait(false);
            throw;
        }
        finally
        {
            lEmployerCancel.Cancel();
            await lEmployerWatch.ConfigureAwait(false);
        }
    }

    private void LEmployerPriorityApply(Process lEmployerProcess)
    {
        if (LEmployerFamily is LCustodyFamily lEmployerFamily)
        {
            LCustody.LCustodyPriorityApply(lEmployerProcess, lEmployerFamily);
            return;
        }

        try
        {
            lEmployerProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
        }
        catch (Exception lEmployerException)
            when (lEmployerException is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }
    }

    private static async Task LEmployerIdleRun(
        Func<long> lEmployerPulseRead, TimeSpan lEmployerLimit, CancellationTokenSource lEmployerCancel)
    {
        try
        {
            while (true)
            {
                await Task.Delay(lEmployerPulseWait, lEmployerCancel.Token).ConfigureAwait(false);
                if (Environment.TickCount64 - lEmployerPulseRead() > lEmployerLimit.TotalMilliseconds)
                {
                    lEmployerCancel.Cancel();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task<bool> LEmployerProcessInterrupt(Process lEmployerProcess)
    {
        try
        {
            if (!lEmployerProcess.HasExited)
            {
                lEmployerProcess.Kill(true);
            }
        }
        catch (Exception lEmployerException)
            when (lEmployerException is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
        }

        try
        {
            using var lEmployerWait = new CancellationTokenSource(lEmployerExitWait);
            await lEmployerProcess.WaitForExitAsync(lEmployerWait.Token).ConfigureAwait(false);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception lEmployerException)
            when (lEmployerException is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return true;
        }
    }

    private static async Task LEmployerLineRead(
        Process lEmployerProcess, Action lEmployerPulse, CancellationToken lEmployerToken, Action<string> lEmployerLine)
    {
        while (await lEmployerProcess.StandardOutput
            .ReadLineAsync(lEmployerToken)
            .ConfigureAwait(false) is string lEmployerText)
        {
            lEmployerPulse();
            lEmployerLine(lEmployerText);
        }
    }

    private static async Task<string> LEmployerErrorRead(
        Process lEmployerProcess, Action lEmployerPulse, Action<string>? lEmployerLine)
    {
        var lEmployerBuilder = new StringBuilder();
        bool lEmployerTruncated = false;
        while (await lEmployerProcess.StandardError
            .ReadLineAsync(CancellationToken.None)
            .ConfigureAwait(false) is string lEmployerText)
        {
            lEmployerPulse();
            lEmployerBuilder.AppendLine(lEmployerText);
            if (lEmployerBuilder.Length > LEmployerErrorLimit * 2)
            {
                lEmployerBuilder.Remove(0, lEmployerBuilder.Length - LEmployerErrorLimit);
                lEmployerTruncated = true;
            }

            lEmployerLine?.Invoke(lEmployerText);
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
