using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaProbe
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, long> LMediaProbeGenerations =
        new(StringComparer.OrdinalIgnoreCase);
    private static long lMediaProbeGeneration;

    internal static Func<string, CancellationToken, LMediaInfo> LMediaProbeReader { get; set; } =
        LMedia.LMediaFfprobeRead;

    internal static int LMediaProbeCount => LMediaProbeGenerations.Count;

    public static event Action<LMediaProbeResult>? LMediaProbeReady;

    public static event Action<LMediaLoudnessResult>? LMediaLoudnessReady;

    public static event Action<bool>? LMediaAvailabilityReady;

    public static void LMediaProbeDefer(string sourcePath, CancellationToken lMediaProbeToken = default)
    {
        long lMediaProbeCurrentGeneration = Interlocked.Increment(ref lMediaProbeGeneration);
        LMediaProbeGenerations[sourcePath] = lMediaProbeCurrentGeneration;

        Task.Run(() =>
        {
            try
            {
                LMediaInfo? lMediaProbeInfo = null;
                string? lMediaProbeError = null;
                try
                {
                    lMediaProbeInfo = LMediaProbeReader(sourcePath, lMediaProbeToken);
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
        Task.Run(() =>
        {
            double? lMediaLoudnessValue = null;
            string? lMediaLoudnessError = null;
            try
            {
                lMediaLoudnessValue = LMedia.LMediaLoudnessRead(sourcePath, lMediaProbeToken);
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
        Task.Run(() =>
        {
            bool lMediaAvailable = LMedia.LMediaFfprobeExist();
            LMediaAvailabilityReady?.Invoke(lMediaAvailable);
        }, lMediaProbeToken);
    }
}
