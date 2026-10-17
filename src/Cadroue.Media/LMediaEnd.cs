using System.Globalization;
using System.IO;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public static class LMediaEnd
{
    private static readonly TimeSpan lMediaEndWindow = TimeSpan.FromMinutes(5);

    internal static async Task<TimeSpan?> LMediaEndRead(
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

        await LMediaSlot.LMediaScanClaim(lMediaToken).ConfigureAwait(false);
        try
        {
            LMediaProcessResult lMediaResult = await LMediaProcess.LMediaProcessRun(
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
            LMediaSlot.LMediaScanRelease();
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
