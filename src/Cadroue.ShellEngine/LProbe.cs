using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LProbe
{
    internal static double LProbeMergeRead(IReadOnlyList<string> lProbeMergeSources)
    {
        double lProbeTotalSeconds = 0;
        foreach (string lProbeMergeSource in lProbeMergeSources)
        {
            lProbeTotalSeconds += LProbeMediaRead(lProbeMergeSource)?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lProbeTotalSeconds;
    }

    internal static LWorkMedia? LProbeMediaRead(string lProbeMediaPath)
    {
        if (string.IsNullOrWhiteSpace(lProbeMediaPath) || !File.Exists(lProbeMediaPath))
        {
            return null;
        }

        try
        {
            LMediaInfo lProbeMedia = LMediaInfo.LMediaFfprobeRead(lProbeMediaPath);
            return new LWorkMedia(
                lProbeMedia.LMediaVideoWidth,
                lProbeMedia.LMediaVideoHeight,
                lProbeMedia.LMediaVideoRate,
                (long)Math.Round(lProbeMedia.LMediaInfoDuration.TotalMilliseconds),
                lProbeMedia.LMediaVideoPresent);
        }
        catch (Exception lProbeException)
        {
            LRunner.LRunnerRecord($"Media could not be read '{Path.GetFileName(lProbeMediaPath)}'", lProbeException);
            return null;
        }
    }

    internal static double? LProbeIntervalRead(string lProbeMediaPath, TimeSpan lProbeMediaDuration)
    {
        if (string.IsNullOrWhiteSpace(lProbeMediaPath) || !File.Exists(lProbeMediaPath) || lProbeMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lProbeKeyframes = LKeyframeSeeker.LKeyframeRangeScan(
                lProbeMediaPath, TimeSpan.Zero, lProbeMediaDuration);
            if (lProbeKeyframes.Count < 2)
            {
                return null;
            }

            double lProbeSpanMilliseconds =
                (lProbeKeyframes[^1].LKeyframePresentationTime - lProbeKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
            return lProbeSpanMilliseconds / (lProbeKeyframes.Count - 1);
        }
        catch (Exception lProbeException)
        {
            LRunner.LRunnerRecord($"Keyframe interval could not be read '{Path.GetFileName(lProbeMediaPath)}'", lProbeException);
            return null;
        }
    }

    internal static long? LProbeInputRead(LWorkItem lProbeWorkItem)
    {
        if (lProbeWorkItem.LWorkMergeSources.Count > 1)
        {
            long lProbeMergeTotal = 0;
            foreach (string lProbeMergeSource in lProbeWorkItem.LWorkMergeSources)
            {
                if (LProbeBytesRead(lProbeMergeSource) is not { } lProbeMergeBytes)
                {
                    lProbeMergeTotal = 0;
                    break;
                }

                lProbeMergeTotal += lProbeMergeBytes;
            }

            if (lProbeMergeTotal > 0)
            {
                return lProbeMergeTotal;
            }
        }

        return lProbeWorkItem.LWorkSourceBytes ?? LProbeBytesRead(lProbeWorkItem.LWorkSourcePath);
    }

    internal static long? LProbeBytesRead(string lProbeOutputPath)
    {
        if (string.IsNullOrWhiteSpace(lProbeOutputPath))
        {
            return null;
        }

        try
        {
            var lProbeOutputFile = new FileInfo(lProbeOutputPath);
            return lProbeOutputFile.Exists ? lProbeOutputFile.Length : null;
        }
        catch (Exception lProbeException) when (lProbeException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
