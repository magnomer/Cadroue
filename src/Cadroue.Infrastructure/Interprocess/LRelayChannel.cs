using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cadroue.Infrastructure;

public enum LRelayOutcome { LRelayOutcomeExisting, LRelayOutcomeLaunched, LRelayOutcomeFailed }

public static class LRelayChannel
{
    public const string LRelayArgument = "--relay";

    private const string LRelayPipePrefix = "Cadroue.Relay.";
    private const string LRelayTabMessage = "TAB";
    private const string LRelayOkReply = "OK";
    private const string LRelayNoReply = "NO";
    private const int LRelayConnectTimeout = 1500;
    private const int LRelayReplyTimeout = 10000;
    private const int LRelayLaunchTimeout = 30000;
    private const int LRelayLaunchPoll = 100;

    private static CancellationTokenSource? lRelayCancellation;
    private static string? lRelayStartupPath;

    public static Func<LRelay, bool>? LRelayAcceptSeam { get; set; }

    public static LRelay? LRelayStartupPayload { get; private set; }

    public static LRelay? LRelayPayloadRead()
    {
        LRelay? lRelayPayload = LRelayStartupPayload;
        LRelayStartupPayload = null;
        return lRelayPayload;
    }

    public static void LRelayStartupRead(string[] lStartupArguments)
    {
        for (int lIndex = 0; lIndex < lStartupArguments.Length - 1; lIndex++)
        {
            if (!string.Equals(lStartupArguments[lIndex], LRelayArgument, StringComparison.Ordinal))
            {
                continue;
            }

            string lRelayFilePath = lStartupArguments[lIndex + 1];
            LRelayStartupPayload = LRelayPayloadLoad(lRelayFilePath);
            if (LRelayStartupPayload is { } lRelayPayload)
            {
                lRelayStartupPath = lRelayFilePath;
                LTraceLog.LTraceInfoRecord($"Started to receive a relayed '{lRelayPayload.LRelayLayoutKey}' tab");
            }

            return;
        }
    }

    public static void LRelayStartupCommit()
    {
        if (lRelayStartupPath is not { } lRelayFilePath)
        {
            return;
        }

        lRelayStartupPath = null;
        LRelayStore.LRelayFileClear(lRelayFilePath);
        LTraceLog.LTraceInfoRecord("Relayed tab committed; the sender may now close its copy");
    }

    public static void LRelayChannelStart()
    {
        if (lRelayCancellation is not null)
        {
            return;
        }

        lRelayCancellation = new CancellationTokenSource();
        var lRelayThread = new Thread(() => LRelayListenRun(lRelayCancellation.Token))
        {
            IsBackground = true,
            Name = "CadroueRelay"
        };
        lRelayThread.Start();
        LTraceLog.LTraceInfoRecord($"Relay channel listening on {LRelayPipeCreate(Environment.ProcessId)}");
    }

    public static void LRelayChannelStop()
    {
        lRelayCancellation?.Cancel();
        lRelayCancellation = null;
    }

    public static int? LRelayInstanceFind(double lScreenLeft, double lScreenTop)
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

        return LRelayPipeCheck(lProcessId) ? lProcessId : null;
    }

    public static bool LRelayChannelSend(int lProcessId, string lRelayFilePath)
    {
        try
        {
            using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayPipeCreate(lProcessId), PipeDirection.InOut);
            lRelayPipe.Connect(LRelayConnectTimeout);

            var lRelayWriter = new StreamWriter(lRelayPipe) { AutoFlush = true };
            var lRelayReader = new StreamReader(lRelayPipe);
            lRelayWriter.WriteLine($"{LRelayTabMessage} {lRelayFilePath}");
            Task<string?> lRelayReply = lRelayReader.ReadLineAsync();
            if (!lRelayReply.Wait(LRelayReplyTimeout))
            {
                LTraceLog.LTraceErrorRecord(
                    $"Relay target {lProcessId} gave no reply within {LRelayReplyTimeout} ms; tab kept", null);
                return false;
            }

            return string.Equals(lRelayReply.Result, LRelayOkReply, StringComparison.Ordinal);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Relay send to process {lProcessId} failed", lException);
            return false;
        }
    }

    public static async Task<LRelayOutcome> LRelayDispatch(string lRelayFilePath, double lScreenLeft, double lScreenTop)
    {
        if (LRelayInstanceFind(lScreenLeft, lScreenTop) is int lTargetProcessId)
        {
            if (await Task.Run(() => LRelayChannelSend(lTargetProcessId, lRelayFilePath)).ConfigureAwait(true))
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
            pRelayStart.ArgumentList.Add(LRelayArgument);
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

    private static bool LRelayPipeCheck(int lProcessId)
    {
        try
        {
            using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayPipeCreate(lProcessId), PipeDirection.InOut);
            lRelayPipe.Connect(200);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void LRelayListenRun(CancellationToken lRelayToken)
    {
        string lRelayPipeName = LRelayPipeCreate(Environment.ProcessId);
        while (!lRelayToken.IsCancellationRequested)
        {
            try
            {
                using var lRelayPipe = new NamedPipeServerStream(
                    lRelayPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                lRelayPipe.WaitForConnectionAsync(lRelayToken).GetAwaiter().GetResult();

                using var lRelayReader = new StreamReader(lRelayPipe);
                using var lRelayWriter = new StreamWriter(lRelayPipe) { AutoFlush = true };
                LRelayMessageHandle(lRelayReader.ReadLine(), lRelayWriter);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception lException)
            {
                LTraceLog.LTraceErrorRecord("Relay listener failed", lException);
            }
        }
    }

    private static void LRelayMessageHandle(string? lRelayMessage, StreamWriter lRelayWriter)
    {
        if (string.IsNullOrWhiteSpace(lRelayMessage))
        {
            return;
        }

        int lRelaySplitIndex = lRelayMessage.IndexOf(' ');
        if (lRelaySplitIndex <= 0)
        {
            return;
        }

        string lRelayVerb = lRelayMessage[..lRelaySplitIndex];
        string lRelayBody = lRelayMessage[(lRelaySplitIndex + 1)..].Trim();

        if (!string.Equals(lRelayVerb, LRelayTabMessage, StringComparison.Ordinal))
        {
            return;
        }

        LRelay? lRelay = LRelayPayloadLoad(lRelayBody);
        if (lRelay is null)
        {
            lRelayWriter.WriteLine(LRelayNoReply);
            return;
        }

        bool lRelayAccepted = LRelayAcceptSeam?.Invoke(lRelay) ?? false;
        lRelayWriter.WriteLine(lRelayAccepted ? LRelayOkReply : LRelayNoReply);
        if (lRelayAccepted)
        {
            LRelayStore.LRelayFileClear(lRelayBody);
        }
    }

    private static LRelay? LRelayPayloadLoad(string lRelayFilePath)
    {
        LRelay? lRelay = LRelayStore.LRelayFileLoad(lRelayFilePath);
        if (lRelay is null || LRelayPayload.LRelayVersionMatch(lRelay))
        {
            return lRelay;
        }

        LTraceLog.LTraceErrorRecord(
            $"Relayed tab refused: sender version '{lRelay.LRelayVersion}' "
            + $"differs from '{LRelayPayload.LRelayVersionRead()}'",
            null);
        return null;
    }

    private static string LRelayPipeCreate(int lProcessId) => $"{LRelayPipePrefix}{lProcessId}";

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
