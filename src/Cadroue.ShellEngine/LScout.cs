using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LScout
{
    internal static double LScoutMergeRead(IReadOnlyList<string> lScoutMergeSources)
    {
        double lScoutTotalSeconds = 0;
        foreach (string lScoutMergeSource in lScoutMergeSources)
        {
            lScoutTotalSeconds += LScoutMediaRead(lScoutMergeSource)?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lScoutTotalSeconds;
    }

    internal static LWorkMedia? LScoutMediaRead(string lScoutMediaPath)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            LMediaInfo lScoutMedia = LMedia.LMediaFfprobeRead(lScoutMediaPath);
            return new LWorkMedia(
                lScoutMedia.LMediaVideoWidth,
                lScoutMedia.LMediaVideoHeight,
                lScoutMedia.LMediaVideoRate,
                (long)Math.Round(lScoutMedia.LMediaInfoDuration.TotalMilliseconds),
                lScoutMedia.LMediaVideoPresent)
            {
                LWorkMediaCodec = lScoutMedia.LMediaAudioCodec,
                LWorkMediaBitrate = lScoutMedia.LMediaAudioBitrate,
                LWorkMediaSamplerate = lScoutMedia.LMediaSampleRate
            };
        }
        catch (Exception lScoutException)
        {
            LRunner.LRunnerRecord($"Media could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
    }

    internal static double? LScoutIntervalRead(string lScoutMediaPath, TimeSpan lScoutMediaDuration)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath) || lScoutMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = LKeyframeSeeker.LKeyframeRangeScan(
                lScoutMediaPath, TimeSpan.Zero, lScoutMediaDuration);
            if (lScoutKeyframes.Count < 2)
            {
                return null;
            }

            double lScoutSpanMilliseconds =
                (lScoutKeyframes[^1].LKeyframePresentationTime - lScoutKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
            return lScoutSpanMilliseconds / (lScoutKeyframes.Count - 1);
        }
        catch (Exception lScoutException)
        {
            LRunner.LRunnerRecord($"Keyframe interval could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
    }

    internal static long? LScoutInputRead(LWorkItem lScoutWorkItem)
    {
        if (lScoutWorkItem.LWorkMergeSources.Count > 1)
        {
            long lScoutMergeTotal = 0;
            foreach (string lScoutMergeSource in lScoutWorkItem.LWorkMergeSources)
            {
                if (LScoutBytesRead(lScoutMergeSource) is not { } lScoutMergeBytes)
                {
                    lScoutMergeTotal = 0;
                    break;
                }

                lScoutMergeTotal += lScoutMergeBytes;
            }

            if (lScoutMergeTotal > 0)
            {
                return lScoutMergeTotal;
            }
        }

        return lScoutWorkItem.LWorkSourceBytes ?? LScoutBytesRead(lScoutWorkItem.LWorkSourcePath);
    }

    internal static long? LScoutBytesRead(string lScoutOutputPath)
    {
        if (string.IsNullOrWhiteSpace(lScoutOutputPath))
        {
            return null;
        }

        try
        {
            var lScoutOutputFile = new FileInfo(lScoutOutputPath);
            return lScoutOutputFile.Exists ? lScoutOutputFile.Length : null;
        }
        catch (Exception lScoutException) when (lScoutException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
