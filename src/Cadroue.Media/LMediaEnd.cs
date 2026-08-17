using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

using Cadroue.Core;

namespace Cadroue.Media;

public static partial class LMedia
{
    private static readonly TimeSpan lMediaEndWindow = TimeSpan.FromMinutes(5);

    public static LMediaInfo LMediaPreviewRead(
        string lMediaSourcePath,
        CancellationToken lMediaToken = default)
    {
        LMediaInfo lMediaInfo = LMediaFfprobeRead(lMediaSourcePath, lMediaToken);
        if (!lMediaInfo.LMediaVideoPresent)
        {
            return lMediaInfo;
        }

        TimeSpan lMediaScanDuration = lMediaInfo.LMediaVideoDuration > TimeSpan.Zero
            ? lMediaInfo.LMediaVideoDuration
            : lMediaInfo.LMediaInfoDuration;
        TimeSpan? lMediaVideoEnd = LMediaEndRead(
            lMediaSourcePath,
            lMediaScanDuration,
            lMediaInfo.LMediaStartTime,
            lMediaToken);
        return lMediaVideoEnd is null ? lMediaInfo : lMediaInfo with { LMediaVideoEnd = lMediaVideoEnd };
    }

    private static TimeSpan? LMediaEndRead(
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
        var lMediaProcessInfo = new ProcessStartInfo(LTool.LToolFfprobeRead())
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        lMediaProcessInfo.ArgumentList.Add("-v");
        lMediaProcessInfo.ArgumentList.Add("error");
        lMediaProcessInfo.ArgumentList.Add("-select_streams");
        lMediaProcessInfo.ArgumentList.Add("v:0");
        lMediaProcessInfo.ArgumentList.Add("-read_intervals");
        lMediaProcessInfo.ArgumentList.Add(
            lMediaScanOrigin.ToString("0.###", CultureInfo.InvariantCulture) + "%");
        lMediaProcessInfo.ArgumentList.Add("-show_packets");
        lMediaProcessInfo.ArgumentList.Add("-show_entries");
        lMediaProcessInfo.ArgumentList.Add("packet=pts_time");
        lMediaProcessInfo.ArgumentList.Add("-of");
        lMediaProcessInfo.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        lMediaProcessInfo.ArgumentList.Add("-i");
        lMediaProcessInfo.ArgumentList.Add(lMediaSourcePath);

        try
        {
            using var lMediaProcess = Process.Start(lMediaProcessInfo);
            if (lMediaProcess is null)
            {
                return null;
            }

            LCustody.LCustodyAttach(lMediaProcess);
            Task<string> lMediaOutput = lMediaProcess.StandardOutput.ReadToEndAsync(lMediaToken);
            Task<string> lMediaError = lMediaProcess.StandardError.ReadToEndAsync(lMediaToken);
            try
            {
                lMediaProcess.WaitForExitAsync(lMediaToken).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                lMediaProcess.Kill(entireProcessTree: true);
                throw;
            }

            string lMediaPacketText = lMediaOutput.GetAwaiter().GetResult();
            _ = lMediaError.GetAwaiter().GetResult();
            return lMediaProcess.ExitCode == 0
                ? LMediaEndParse(lMediaPacketText, lMediaStart)
                : null;
        }
        catch (Exception lMediaException) when (
            lMediaException is System.ComponentModel.Win32Exception or InvalidOperationException or IOException)
        {
            return null;
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
