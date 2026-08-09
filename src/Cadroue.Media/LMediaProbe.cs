using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaProbe
{
    public static event Action<LMediaProbeResult>? LMediaProbeReady;

    public static event Action<LMediaLoudnessResult>? LMediaLoudnessReady;

    public static event Action<bool>? LMediaAvailabilityReady;

    public static void LMediaProbeDefer(string sourcePath, CancellationToken lMediaProbeToken = default)
    {
        Task.Run(() =>
        {
            LMediaInfo? lMediaProbeInfo = null;
            string? lMediaProbeError = null;
            try
            {
                lMediaProbeInfo = LMedia.LMediaFfprobeRead(sourcePath, lMediaProbeToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception lMediaProbeException)
            {
                lMediaProbeError = lMediaProbeException.Message;
            }

            LMediaProbeReady?.Invoke(new LMediaProbeResult(sourcePath, lMediaProbeInfo, lMediaProbeError));
        }, lMediaProbeToken);
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
