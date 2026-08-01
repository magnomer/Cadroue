using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cadroue.Infrastructure;

public static class LRelayChannel
{
    private const string LRelayPipePrefix = "Cadroue.Relay.";
    private const string LRelayTabMessage = "TAB";
    private const string LRelayOkReply = "OK";
    private const string LRelayNoReply = "NO";
    private const int LRelayConnectTimeout = 1500;

    private static CancellationTokenSource? lRelayCancellation;

    public static event Action<LRelay>? LRelayTabReceive;

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
            return string.Equals(lRelayReader.ReadLine(), LRelayOkReply, StringComparison.Ordinal);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord($"Relay send to process {lProcessId} failed", lException);
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

        LRelay? lRelay = LRelayStore.LRelayFileLoad(lRelayBody);
        if (lRelay is null)
        {
            lRelayWriter.WriteLine(LRelayNoReply);
            return;
        }

        lRelayWriter.WriteLine(LRelayOkReply);
        LRelayStore.LRelayFileClear(lRelayBody);
        LRelayTabReceive?.Invoke(lRelay);
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
