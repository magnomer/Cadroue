using System.Globalization;
using System.IO;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public static partial class LMedia
{
    private static readonly TimeSpan lMediaEndWindow = TimeSpan.FromMinutes(5);

    public static async Task<LMediaInfo> LMediaPreviewRead(
        string lMediaSourcePath,
        CancellationToken lMediaToken = default,
        bool lMediaEndScan = true)
    {
        LMediaInfo lMediaInfo = await LMediaFfprobeRead(lMediaSourcePath, lMediaToken).ConfigureAwait(false);
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
        TimeSpan? lMediaVideoEnd = await LMediaEndRead(
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

    private static async Task<TimeSpan?> LMediaEndRead(
        string lMediaSourcePath,
        TimeSpan lMediaDuration,
        TimeSpan lMediaStart,
        CancellationToken lMediaToken)
    {
        if (lMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        double lMediaScanOrigin = Math.Max(
            0,
            (lMediaStart + lMediaDuration - lMediaEndWindow).TotalSeconds);
        string[] lMediaArguments =
        [
            "-v", "error",
            "-select_streams", "v:0",
            "-read_intervals", lMediaScanOrigin.ToString("0.###", CultureInfo.InvariantCulture) + "%",
            "-show_packets",
            "-show_entries", "packet=pts_time",
            "-of", "default=noprint_wrappers=1:nokey=1",
            "-i", lMediaSourcePath
        ];

        await LMediaScanClaim(lMediaToken).ConfigureAwait(false);
        try
        {
            LMediaProcessResult lMediaResult = await LMediaProcessRun(
                LTool.LToolFfprobeRead(),
                lMediaArguments,
                lMediaToken,
                LCustodyFamily.LCustodyFamilyBackground).ConfigureAwait(false);
            return !lMediaResult.LMediaProcessStalled && lMediaResult.LMediaProcessExit == 0
                ? LMediaEndParse(lMediaResult.LMediaProcessOutput, lMediaStart)
                : null;
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
        }
        finally
        {
            LMediaScanRelease();
        }
    }

    internal static TimeSpan? LMediaEndParse(string lMediaPacketText, TimeSpan lMediaStart)
    {
        double? lMediaLastSeconds = null;
        foreach (string lMediaLine in lMediaPacketText.Split('\n'))
        {
            if (!double.TryParse(
                lMediaLine.Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double lMediaSeconds))
            {
                continue;
            }

            if (lMediaLastSeconds is null || lMediaSeconds > lMediaLastSeconds)
            {
                lMediaLastSeconds = lMediaSeconds;
            }
        }

        if (lMediaLastSeconds is null)
        {
            return null;
        }

        double lMediaRelativeSeconds = lMediaLastSeconds.Value - lMediaStart.TotalSeconds;
        return lMediaRelativeSeconds < 0 ? null : TimeSpan.FromSeconds(lMediaRelativeSeconds);
    }
}
