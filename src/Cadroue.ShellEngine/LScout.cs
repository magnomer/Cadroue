using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LScout
{
    internal static async Task<double> LScoutMergeRead(
        IReadOnlyList<string> lScoutMergeSources,
        CancellationToken lScoutToken = default)
    {
        double lScoutTotalSeconds = 0;
        foreach (string lScoutMergeSource in lScoutMergeSources)
        {
            LWorkMedia? lScoutMedia = await LScoutMediaRead(lScoutMergeSource, lScoutToken).ConfigureAwait(false);
            lScoutTotalSeconds += lScoutMedia?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lScoutTotalSeconds;
    }

    internal static async Task<LWorkMedia?> LScoutMediaRead(
        string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            LMediaInfo lScoutMedia = await LMedia.LMediaFfprobeRead(lScoutMediaPath, lScoutToken).ConfigureAwait(false);
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
            LRunner.LRunnerRecord($"Media could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
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
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = await LKeyframeSeeker.LKeyframeRangeScan(
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
            LRunner.LRunnerRecord(
                $"Keyframe interval could not be read '{Path.GetFileName(lScoutMediaPath)}'",
                lScoutException);
            return null;
        }
    }

    internal static async Task<bool> LScoutDecodeCheck(
        LRunner lScoutRunner, string lScoutOutputPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutOutputPath) || !File.Exists(lScoutOutputPath))
        {
            return false;
        }

        string lScoutBaseArguments = "-hide_banner -nostdin -v error -xerror "
            + $"-i {LEncode.LEncodeFormat(lScoutOutputPath)} -f null -";
        string lScoutArguments = lScoutRunner.LRunnerArgumentTransform?.Invoke(lScoutBaseArguments)
            ?? lScoutBaseArguments;

        try
        {
            var lScoutEmployer = new LEmployer(
                lScoutRunner.LRunnerProgramPath, lScoutRunner.LRunnerArgumentPrefix)
            {
                LEmployerFamily = LCustodyFamily.LCustodyFamilyEncode
            };
            LEmployerResult lScoutResult = await lScoutEmployer.LEmployerRun(
                lScoutArguments,
                lScoutToken,
                static _ => { },
                static _ => { },
                static _ => { }).ConfigureAwait(false);
            lScoutToken.ThrowIfCancellationRequested();
            return lScoutResult.LEmployerExit == 0
                && string.IsNullOrWhiteSpace(lScoutResult.LEmployerError);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            lScoutToken.ThrowIfCancellationRequested();
            return false;
        }
    }

    internal static long? LScoutInputRead(LWorkItem lScoutWorkItem, CancellationToken lScoutToken = default)
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
            return await LMedia.LMediaLoudnessRead(lScoutMediaPath, lScoutToken).ConfigureAwait(false);
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Loudness could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
            return null;
        }
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
        catch (Exception lScoutException)
            when (lScoutException is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }
}
