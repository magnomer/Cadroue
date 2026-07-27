using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace Cadroue.UIShell.PControlBar;

internal static class LRelayChannel
{
    private const string LRelayPipePrefix = "Cadroue.Relay.";
    private const string LRelayTabMessage = "TAB";
    private const string LRelayAckMessage = "ACK";
    private const string LRelayOkReply = "OK";
    private const string LRelayNoReply = "NO";
    private const int LRelayConnectTimeoutMs = 1500;

    private static CancellationTokenSource? lRelayCancellation;

    internal static event Action<LRelay>? LRelayTabReceive;

    internal static event Action<string>? LRelayAckReceive;

    internal static void LRelayChannelStart()
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
        LAppLog.LInfo($"Relay channel listening on {LRelayPipeNameCreate(Environment.ProcessId)}");
    }

    internal static void LRelayChannelStop()
    {
        lRelayCancellation?.Cancel();
        lRelayCancellation = null;
    }

    internal static int? LRelayInstanceFind(double lScreenLeft, double lScreenTop)
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

    internal static bool LRelayChannelSend(int lProcessId, string lRelayFilePath)
    {
        try
        {
            using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayPipeNameCreate(lProcessId), PipeDirection.InOut);
            lRelayPipe.Connect(LRelayConnectTimeoutMs);

            var lRelayWriter = new StreamWriter(lRelayPipe) { AutoFlush = true };
            var lRelayReader = new StreamReader(lRelayPipe);
            lRelayWriter.WriteLine($"{LRelayTabMessage} {lRelayFilePath}");
            return string.Equals(lRelayReader.ReadLine(), LRelayOkReply, StringComparison.Ordinal);
        }
        catch (Exception lException)
        {
            LAppLog.LError($"Relay send to process {lProcessId} failed", lException);
            return false;
        }
    }

    internal static void LRelayAckSend(int lProcessId, string lRelayId)
    {
        if (lProcessId == 0 || lProcessId == Environment.ProcessId)
        {
            return;
        }

        try
        {
            using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayPipeNameCreate(lProcessId), PipeDirection.InOut);
            lRelayPipe.Connect(LRelayConnectTimeoutMs);
            var lRelayWriter = new StreamWriter(lRelayPipe) { AutoFlush = true };
            lRelayWriter.WriteLine($"{LRelayAckMessage} {lRelayId}");
            lRelayWriter.Flush();
        }
        catch (Exception lException)
        {
            LAppLog.LError($"Relay acknowledgement to process {lProcessId} failed", lException);
        }
    }

    private static bool LRelayPipeCheck(int lProcessId)
    {
        try
        {
            using var lRelayPipe = new NamedPipeClientStream(
                ".", LRelayPipeNameCreate(lProcessId), PipeDirection.InOut);
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
        string lRelayPipeName = LRelayPipeNameCreate(Environment.ProcessId);
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
                LAppLog.LError("Relay listener failed", lException);
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

        if (string.Equals(lRelayVerb, LRelayAckMessage, StringComparison.Ordinal))
        {
            LRelayDispatch(() => LRelayAckReceive?.Invoke(lRelayBody));
            return;
        }

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
        LRelayDispatch(() => LRelayTabReceive?.Invoke(lRelay));
    }

    private static void LRelayDispatch(Action lRelayAction)
    {
        Application? lRelayApplication = Application.Current;
        if (lRelayApplication is null)
        {
            return;
        }

        lRelayApplication.Dispatcher.BeginInvoke(lRelayAction);
    }

    private static string LRelayPipeNameCreate(int lProcessId) => $"{LRelayPipePrefix}{lProcessId}";

    private const uint LRelayAncestorRoot = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct LRelayPoint
    {
        public int X;
        public int Y;

        public LRelayPoint(int lPointX, int lPointY)
        {
            X = lPointX;
            Y = lPointY;
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(LRelayPoint pointScreen);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr windowHandle, uint ancestorFlag);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out int processId);
}
