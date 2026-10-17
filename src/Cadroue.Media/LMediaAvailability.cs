namespace Cadroue.Media;

public static class LMediaAvailability
{
    private static bool? lMediaAvailable;

    public static event Action<bool>? LMediaAvailabilityReady;

    public static bool? LMediaAvailabilityCurrent => lMediaAvailable;

    public static void LMediaAvailabilityDefer(CancellationToken lMediaProbeToken = default)
    {
        Task.Run(async () =>
        {
            bool lMediaAvailableNow = await LMediaFfprobe.LMediaFfprobeExist().ConfigureAwait(false);
            lMediaAvailable = lMediaAvailableNow;
            LMediaAvailabilityReady?.Invoke(lMediaAvailableNow);
        }, lMediaProbeToken);
    }
}
