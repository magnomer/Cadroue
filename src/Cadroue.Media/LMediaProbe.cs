using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaProbe
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> LMediaProbeGenerations =
        new(StringComparer.OrdinalIgnoreCase);
    private static long lMediaProbeGeneration;
    private static bool? lMediaAvailable;

    internal static Func<string, CancellationToken, Task<LMediaInfo>> LMediaProbeReader { get; set; } =
        LMedia.LMediaFfprobeRead;

    internal static int LMediaProbeCount => LMediaProbeGenerations.Count;

    public static event Action<LMediaProbeResult>? LMediaProbeReady;

    public static event Action<LMediaLoudnessResult>? LMediaLoudnessReady;

    public static event Action<bool>? LMediaAvailabilityReady;

    public static bool? LMediaAvailabilityCurrent => lMediaAvailable;

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

    public static void LMediaLoudnessDefer(string sourcePath, CancellationToken lMediaProbeToken = default)
    {
        Task.Run(async () =>
        {
            double? lMediaLoudnessValue = null;
            string? lMediaLoudnessError = null;
            try
            {
                lMediaLoudnessValue = await LMedia.LMediaLoudnessRead(sourcePath, lMediaProbeToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception lMediaLoudnessException)
            {
                lMediaLoudnessError = lMediaLoudnessException.Message;
            }

            LMediaLoudnessReady?.Invoke(new LMediaLoudnessResult(sourcePath, lMediaLoudnessValue, lMediaLoudnessError));
        }, lMediaProbeToken);
    }

    public static void LMediaAvailabilityDefer(CancellationToken lMediaProbeToken = default)
    {
        Task.Run(async () =>
        {
            bool lMediaAvailableNow = await LMedia.LMediaFfprobeExist().ConfigureAwait(false);
            lMediaAvailable = lMediaAvailableNow;
            LMediaAvailabilityReady?.Invoke(lMediaAvailableNow);
        }, lMediaProbeToken);
    }
}
