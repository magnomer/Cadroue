using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LSidecarDuration
{
    public static TimeSpan LSidecarDurationRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarField.LSidecarCoreRead(lSidecarSourcePath)
                    is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarCore
                ? TimeSpan.FromMilliseconds(lSidecarCore.LSidecarSource.LSidecarDurationMilliseconds)
                : TimeSpan.Zero;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return TimeSpan.Zero;
        }
    }

    public static async Task<TimeSpan> LSidecarDurationResolve(string lSidecarSourcePath)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarPath.LSidecarPathRead(lSidecarSourcePath);
            LSidecarCoreDocument? lSidecarCore;
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                lSidecarCore = LSidecarField.LSidecarCoreRead(lSidecarSourcePath);
            }

            if (lSidecarCore is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarKnown)
            {
                return TimeSpan.FromMilliseconds(lSidecarKnown.LSidecarSource.LSidecarDurationMilliseconds);
            }

            TimeSpan lSidecarProbed;
            try
            {
                lSidecarProbed = (await LMediaFfprobe.LMediaFfprobeRead(lSidecarSourcePath).ConfigureAwait(false))
                    .LMediaInfoDuration;
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }

            if (lSidecarProbed <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            LSidecarCore.LSidecarCoreSave(
                lSidecarSourcePath,
                lSidecarTarget => lSidecarTarget.LSidecarSource.LSidecarDurationMilliseconds =
                    (long)Math.Round(lSidecarProbed.TotalMilliseconds));

            return lSidecarProbed;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or TimeoutException)
        {
            return TimeSpan.Zero;
        }
    }
}
