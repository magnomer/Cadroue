using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cadroue.Infrastructure;

public enum LRelayOutcome { LRelayOutcomeExisting, LRelayOutcomeLaunched, LRelayOutcomeFailed }

public static class LRelayDelivery
{
    private const int LRelayConnectTimeout = 1500;
    private const int LRelayReplyTimeout = 10000;
    private const int LRelayLaunchTimeout = 30000;
    private const int LRelayLaunchPoll = 100;

    public static async Task<int?> LRelayInstanceFind(double lScreenLeft, double lScreenTop)
    {
        IntPtr lWindowHandle = WindowFromPoint(new LRelayPoint((int)lScreenLeft, (int)lScreenTop));
        if (lWindowHandle == IntPtr.Zero)
        {
            return null;
        }

        IntPtr lRootHandle = GetAncestor(lWindowHandle, LRelayAncestorRoot);
        if (lRootHandle == IntPtr.Zero)
        {
            lRootHandle = lWindowHandle;
        }

        _ = GetWindowThreadProcessId(lRootHandle, out int lProcessId);
        if (lProcessId == 0 || lProcessId == Environment.ProcessId)
        {
            return null;
        }

        return await LRelayPipeCheck(lProcessId).ConfigureAwait(false) ? lProcessId : null;
    }

    public static async Task<bool> LRelayChannelSend(int lProcessId, string lRelayFilePath)
    {
        try
        {
            await using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayChannel.LRelayPipeCreate(lProcessId), PipeDirection.InOut, PipeOptions.Asynchronous);
            await lRelayPipe.ConnectAsync(LRelayConnectTimeout).ConfigureAwait(false);

            var lRelayWriter = new StreamWriter(lRelayPipe) { AutoFlush = true };
            var lRelayReader = new StreamReader(lRelayPipe);
            await lRelayWriter.WriteLineAsync($"{LRelayChannel.LRelayTabMessage} {lRelayFilePath}")
                .ConfigureAwait(false);
            using var lRelayLimit = new CancellationTokenSource(LRelayReplyTimeout);
            try
            {
                string? lRelayReply = await lRelayReader.ReadLineAsync(lRelayLimit.Token).ConfigureAwait(false);
                return string.Equals(lRelayReply, LRelayChannel.LRelayOkReply, StringComparison.Ordinal);
            }
            catch (OperationCanceledException)
            {
                LTraceLog.LTraceErrorRecord(
                    $"Relay target {lProcessId} gave no reply within {LRelayReplyTimeout} ms; tab kept", null);
                return false;
            }
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Relay send to process {lProcessId} failed", lException);
            return false;
        }
    }

    public static async Task<LRelayOutcome> LRelayDispatch(string lRelayFilePath, double lScreenLeft, double lScreenTop)
    {
        if (await LRelayInstanceFind(lScreenLeft, lScreenTop).ConfigureAwait(true) is int lTargetProcessId)
        {
            if (await LRelayChannelSend(lTargetProcessId, lRelayFilePath).ConfigureAwait(true))
            {
                return LRelayOutcome.LRelayOutcomeExisting;
            }

            LRelayStore.LRelayFileClear(lRelayFilePath);
            LTraceLog.LTraceErrorRecord($"Relay target {lTargetProcessId} refused the tab; tab kept", null);
            return LRelayOutcome.LRelayOutcomeFailed;
        }

        if (LRelayInstanceStart(lRelayFilePath) is { } lRelayProcess
            && await LRelayLaunchCheck(lRelayProcess, lRelayFilePath).ConfigureAwait(true))
        {
            return LRelayOutcome.LRelayOutcomeLaunched;
        }

        LRelayStore.LRelayFileClear(lRelayFilePath);
        return LRelayOutcome.LRelayOutcomeFailed;
    }

    public static Process? LRelayInstanceStart(string lRelayFilePath)
    {
        string? pRelayProgramPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(pRelayProgramPath))
        {
            LTraceLog.LTraceErrorRecord("Relay launch skipped: program path unknown", null);
            return null;
        }

        try
        {
            var pRelayStart = new ProcessStartInfo(pRelayProgramPath) { UseShellExecute = false };
            pRelayStart.ArgumentList.Add(LRelayStartup.LRelayArgument);
            pRelayStart.ArgumentList.Add(lRelayFilePath);
            return Process.Start(pRelayStart);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Relay launch failed; tab kept", lException);
            return null;
        }
    }

    private static async Task<bool> LRelayLaunchCheck(Process lRelayProcess, string lRelayFilePath)
    {
        using (lRelayProcess)
        {
            var lRelayClock = Stopwatch.StartNew();
            while (lRelayClock.ElapsedMilliseconds < LRelayLaunchTimeout)
            {
                if (!File.Exists(lRelayFilePath))
                {
                    return true;
                }

                if (lRelayProcess.HasExited)
                {
                    LTraceLog.LTraceErrorRecord(
                        $"Relay instance exited with code {lRelayProcess.ExitCode} before taking the tab; "
                        + "tab kept",
                        null);
                    return false;
                }

                await Task.Delay(LRelayLaunchPoll).ConfigureAwait(true);
            }

            LTraceLog.LTraceErrorRecord(
                $"Relay instance did not take the tab within {LRelayLaunchTimeout} ms; tab kept", null);
            return false;
        }
    }

    private static async Task<bool> LRelayPipeCheck(int lProcessId)
    {
        try
        {
            await using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayChannel.LRelayPipeCreate(lProcessId), PipeDirection.InOut, PipeOptions.Asynchronous);
            await lRelayPipe.ConnectAsync(200).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private const uint LRelayAncestorRoot = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct LRelayPoint
    {
        public int LRelayX;
        public int LRelayY;

        public LRelayPoint(int lPointX, int lPointY)
        {
            LRelayX = lPointX;
            LRelayY = lPointY;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(LRelayPoint pointScreen);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr windowHandle, uint ancestorFlag);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out int processId);
}
