using System.Threading;

namespace Cadroue.Media;

public static class LMediaSlot
{
    private static readonly SemaphoreSlim lMediaScanSlot = new(1, 1);
    private static int lMediaScanWaiters;

    public static async Task LMediaScanClaim(CancellationToken lMediaToken = default)
    {
        if (Volatile.Read(ref lMediaScanWaiters) > 0)
        {
            await Task.Delay(1, lMediaToken).ConfigureAwait(false);
        }

        Interlocked.Increment(ref lMediaScanWaiters);
        try
        {
            await lMediaScanSlot.WaitAsync(lMediaToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref lMediaScanWaiters);
        }
    }

    public static void LMediaScanRelease() => lMediaScanSlot.Release();
}
