using System.IO;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaPreview
{
    public static async Task<LMediaInfo> LMediaPreviewRead(
        string lMediaSourcePath,
        CancellationToken lMediaToken = default,
        bool lMediaEndScan = true)
    {
        LMediaInfo lMediaInfo = await LMediaFfprobe.LMediaFfprobeRead(lMediaSourcePath, lMediaToken)
            .ConfigureAwait(false);
        if (!lMediaEndScan || !lMediaInfo.LMediaVideoPresent)
        {
            return lMediaInfo;
        }

        LKeyframeSourceIdentity? lMediaIdentity = LMediaIdentityCreate(lMediaSourcePath, lMediaInfo.LMediaInfoDuration);
        if (lMediaIdentity is not null && LMediaEndCache.LMediaEndLoad(lMediaIdentity, out TimeSpan? lMediaCachedEnd))
        {
            return lMediaCachedEnd is null ? lMediaInfo : lMediaInfo with { LMediaVideoEnd = lMediaCachedEnd };
        }

        TimeSpan lMediaScanDuration = lMediaInfo.LMediaVideoDuration > TimeSpan.Zero
            ? lMediaInfo.LMediaVideoDuration
            : lMediaInfo.LMediaInfoDuration;
        TimeSpan? lMediaVideoEnd = await LMediaEnd.LMediaEndRead(
            lMediaSourcePath,
            lMediaScanDuration,
            lMediaInfo.LMediaStartTime,
            lMediaToken).ConfigureAwait(false);
        if (lMediaIdentity is not null)
        {
            LMediaEndCache.LMediaEndSave(lMediaIdentity, lMediaVideoEnd);
        }

        return lMediaVideoEnd is null ? lMediaInfo : lMediaInfo with { LMediaVideoEnd = lMediaVideoEnd };
    }

    private static LKeyframeSourceIdentity? LMediaIdentityCreate(string lMediaSourcePath, TimeSpan lMediaDuration)
    {
        try
        {
            return LKeyframeSourceIdentity.LKeyframeIdentityCreate(lMediaSourcePath, lMediaDuration);
        }
        catch (Exception lMediaException)
            when (lMediaException is ArgumentException
                or FileNotFoundException
                or IOException
                or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
