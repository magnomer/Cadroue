using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LScout
{
    internal static async Task<double> LScoutMergeRead(
        IReadOnlyList<string> lScoutMergeSources,
        CancellationToken lScoutToken = default,
        int? lScoutLevel = null)
    {
        double lScoutTotalSeconds = 0;
        foreach (string lScoutMergeSource in lScoutMergeSources)
        {
            LWorkMedia? lScoutMedia = await LScoutMediaRead(lScoutMergeSource, lScoutToken, lScoutLevel)
                .ConfigureAwait(false);
            lScoutTotalSeconds += lScoutMedia?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lScoutTotalSeconds;
    }

    internal static async Task<LWorkMedia?> LScoutMediaRead(
        string lScoutMediaPath, CancellationToken lScoutToken = default, int? lScoutLevel = null)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            LMediaInfo lScoutMedia = await LMediaEngine.LMediaProbeSeam(lScoutMediaPath, lScoutToken, lScoutLevel)
                .ConfigureAwait(false);
            return new LWorkMedia(
                lScoutMedia.LMediaVideoWidth,
                lScoutMedia.LMediaVideoHeight,
                lScoutMedia.LMediaVideoRate,
                (long)Math.Round(lScoutMedia.LMediaInfoDuration.TotalMilliseconds),
                lScoutMedia.LMediaVideoPresent)
            {
                LWorkMediaCodec = lScoutMedia.LMediaVideoCodec,
                LWorkAudioCodec = lScoutMedia.LMediaAudioCodec,
                LWorkMediaBitrate = lScoutMedia.LMediaAudioBitrate,
                LWorkMediaSamplerate = lScoutMedia.LMediaSampleRate,
                LWorkMediaPixel = lScoutMedia.LMediaVideoPixel,
                LWorkMediaRange = lScoutMedia.LMediaVideoRange
            };
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Media could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
    }

    internal static async Task<double?> LScoutIntervalRead(
        string lScoutMediaPath,
        LWorkMedia lScoutMedia,
        CancellationToken lScoutToken = default)
    {
        TimeSpan lScoutMediaDuration = lScoutMedia.LWorkMediaDuration;
        if (string.IsNullOrWhiteSpace(lScoutMediaPath)
            || !File.Exists(lScoutMediaPath)
            || lScoutMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        if (LKeyframeCodec.LKeyframeKindResolve(lScoutMedia.LWorkMediaCodec) == LKeyframeKind.LKeyframeKindIntra)
        {
            return lScoutMedia.LWorkMediaFramerate > 0 ? 1000d / lScoutMedia.LWorkMediaFramerate : null;
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = await LKeyframeEngine.LKeyframeScanSeam(
                lScoutMediaPath, TimeSpan.Zero, lScoutMediaDuration, lScoutToken).ConfigureAwait(false);
            if (lScoutKeyframes.Count < 2)
            {
                return null;
            }

            double lScoutSpanMilliseconds =
                (lScoutKeyframes[^1].LKeyframePresentationTime
                    - lScoutKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
            return lScoutSpanMilliseconds / (lScoutKeyframes.Count - 1);
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Keyframe interval could not be read '{Path.GetFileName(lScoutMediaPath)}'",
                lScoutException);
            return null;
        }
    }

    internal static async Task<LWorkMedia?> LScoutSourceRead(
        string lScoutSourcePath, CancellationToken lScoutToken = default)
    {
        if (await LScoutMediaRead(lScoutSourcePath, lScoutToken).ConfigureAwait(false) is not { } lScoutMedia)
        {
            return null;
        }

        if (lScoutMedia.LWorkMediaVideo
            && await LScoutIntervalRead(lScoutSourcePath, lScoutMedia, lScoutToken).ConfigureAwait(false)
                is { } lScoutInterval)
        {
            lScoutMedia = lScoutMedia with { LWorkKeyframeInterval = lScoutInterval };
        }

        if (lScoutMedia.LWorkMediaSamplerate > 0
            && await LScoutLoudnessRead(lScoutSourcePath, lScoutToken).ConfigureAwait(false) is { } lScoutLoudness)
        {
            lScoutMedia = lScoutMedia with { LWorkMediaLoudness = lScoutLoudness };
        }

        return lScoutMedia;
    }

    internal static async Task<double?> LScoutLoudnessRead(
        string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            return await LMediaEngine.LMediaLoudnessSeam(lScoutMediaPath, lScoutToken).ConfigureAwait(false);
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunnerTrace.LRunnerRecord(
                $"Loudness could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
    }
}
