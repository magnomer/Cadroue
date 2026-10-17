using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaProbe
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> LMediaProbeGenerations =
        new(StringComparer.OrdinalIgnoreCase);
    private static long lMediaProbeGeneration;

    internal static Func<string, CancellationToken, Task<LMediaInfo>> LMediaProbeReader { get; set; } =
        LMediaFfprobe.LMediaFfprobeRead;

    internal static int LMediaProbeCount => LMediaProbeGenerations.Count;

    public static event Action<LMediaProbeResult>? LMediaProbeReady;

    public static void LMediaProbeDefer(string sourcePath, CancellationToken lMediaProbeToken = default)
    {
        long lMediaProbeCurrentGeneration = Interlocked.Increment(ref lMediaProbeGeneration);
        LMediaProbeGenerations[sourcePath] = lMediaProbeCurrentGeneration;

        Task.Run(async () =>
        {
            try
            {
                LMediaInfo? lMediaProbeInfo = null;
                string? lMediaProbeError = null;
                try
                {
                    lMediaProbeInfo = await LMediaProbeReader(sourcePath, lMediaProbeToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception lMediaProbeException)
                {
                    lMediaProbeError = lMediaProbeException.Message;
                }

                if (lMediaProbeToken.IsCancellationRequested
                    || !LMediaProbeGenerations.TryGetValue(sourcePath, out long lMediaProbeLatestGeneration)
                    || lMediaProbeLatestGeneration != lMediaProbeCurrentGeneration)
                {
                    return;
                }

                LMediaProbeReady?.Invoke(new LMediaProbeResult(sourcePath, lMediaProbeInfo, lMediaProbeError));
            }
            finally
            {
                LMediaProbeGenerations.TryRemove(
                    new KeyValuePair<string, long>(sourcePath, lMediaProbeCurrentGeneration));
            }
        }, CancellationToken.None);
    }
}
