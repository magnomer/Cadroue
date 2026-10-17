using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace Cadroue.Infrastructure;

public static class LRelayChannel
{
    internal const string LRelayTabMessage = "TAB";
    internal const string LRelayOkReply = "OK";

    private const string LRelayPipePrefix = "Cadroue.Relay.";
    private const string LRelayNoReply = "NO";

    private static CancellationTokenSource? lRelayCancellation;

    public static Func<LRelay, bool>? LRelayAcceptSeam { get; set; }

    public static void LRelayChannelStart()
    {
        if (lRelayCancellation is not null)
        {
            return;
        }

        lRelayCancellation = new CancellationTokenSource();
        CancellationToken lRelayToken = lRelayCancellation.Token;
        _ = Task.Run(() => LRelayListenRun(lRelayToken), CancellationToken.None);
        LTraceLog.LTraceInfoRecord($"Relay channel listening on {LRelayPipeCreate(Environment.ProcessId)}");
    }

    public static void LRelayChannelStop()
    {
        lRelayCancellation?.Cancel();
        lRelayCancellation = null;
    }

    private static async Task LRelayListenRun(CancellationToken lRelayToken)
    {
        string lRelayPipeName = LRelayPipeCreate(Environment.ProcessId);
        while (!lRelayToken.IsCancellationRequested)
        {
            try
            {
                await using var lRelayPipe = new NamedPipeServerStream(
                    lRelayPipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                await lRelayPipe.WaitForConnectionAsync(lRelayToken).ConfigureAwait(false);

                using var lRelayReader = new StreamReader(lRelayPipe);
                await using var lRelayWriter = new StreamWriter(lRelayPipe) { AutoFlush = true };
                string? lRelayMessage = await lRelayReader.ReadLineAsync(lRelayToken).ConfigureAwait(false);
                string? lRelayReply = LRelayMessageHandle(lRelayMessage);
                if (lRelayReply is not null)
                {
                    await lRelayWriter.WriteLineAsync(lRelayReply).ConfigureAwait(false);
                }
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

    private static string? LRelayMessageHandle(string? lRelayMessage)
    {
        if (string.IsNullOrWhiteSpace(lRelayMessage))
        {
            return null;
        }

        int lRelaySplitIndex = lRelayMessage.IndexOf(' ');
        if (lRelaySplitIndex <= 0)
        {
            return null;
        }

        string lRelayVerb = lRelayMessage[..lRelaySplitIndex];
        string lRelayBody = lRelayMessage[(lRelaySplitIndex + 1)..].Trim();

        if (!string.Equals(lRelayVerb, LRelayTabMessage, StringComparison.Ordinal))
        {
            return null;
        }

        LRelay? lRelay = LRelayPayload.LRelayPayloadLoad(lRelayBody);
        if (lRelay is null)
        {
            return LRelayNoReply;
        }

        bool lRelayAccepted = LRelayAcceptSeam?.Invoke(lRelay) ?? false;
        if (lRelayAccepted)
        {
            LRelayStore.LRelayFileClear(lRelayBody);
        }

        return lRelayAccepted ? LRelayOkReply : LRelayNoReply;
    }

    internal static string LRelayPipeCreate(int lProcessId) => $"{LRelayPipePrefix}{lProcessId}";
}
