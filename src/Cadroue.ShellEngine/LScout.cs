using System.Diagnostics;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.ShellEngine;

internal static class LScout
{
    internal static double LScoutMergeRead(IReadOnlyList<string> lScoutMergeSources, CancellationToken lScoutToken = default)
    {
        double lScoutTotalSeconds = 0;
        foreach (string lScoutMergeSource in lScoutMergeSources)
        {
            lScoutTotalSeconds += LScoutMediaRead(lScoutMergeSource, lScoutToken)?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return lScoutTotalSeconds;
    }

    internal static LWorkMedia? LScoutMediaRead(string lScoutMediaPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath))
        {
            return null;
        }

        try
        {
            LMediaInfo lScoutMedia = LMedia.LMediaFfprobeRead(lScoutMediaPath, lScoutToken);
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

    internal static double? LScoutIntervalRead(string lScoutMediaPath, TimeSpan lScoutMediaDuration, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutMediaPath) || !File.Exists(lScoutMediaPath) || lScoutMediaDuration <= TimeSpan.Zero)
        {
            return null;
        }

        try
        {
            IReadOnlyList<LKeyframeEntry> lScoutKeyframes = LKeyframeSeeker.LKeyframeRangeScan(
                lScoutMediaPath, TimeSpan.Zero, lScoutMediaDuration, lScoutToken);
            if (lScoutKeyframes.Count < 2)
            {
                return null;
            }

            double lScoutSpanMilliseconds =
                (lScoutKeyframes[^1].LKeyframePresentationTime - lScoutKeyframes[0].LKeyframePresentationTime).TotalMilliseconds;
            return lScoutSpanMilliseconds / (lScoutKeyframes.Count - 1);
        }
        catch (Exception lScoutException) when (lScoutException is not OperationCanceledException)
        {
            LRunner.LRunnerRecord($"Keyframe interval could not be read '{Path.GetFileName(lScoutMediaPath)}'", lScoutException);
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

        // Read-only decode-to-null. Never writes the output; only re-runs the
        // affected operation to confirm the repaired stream decodes cleanly.
        // Routed through the runner's configured program path, argument prefix,
        // and argument transform so validation and repair use the identical ffmpeg.
        string lScoutBaseArguments = "-hide_banner -nostdin -v error -xerror "
            + $"-i {LEncode.LEncodeFormat(lScoutOutputPath)} -f null -";
        string lScoutArguments = lScoutRunner.LRunnerArgumentTransform?.Invoke(lScoutBaseArguments)
            ?? lScoutBaseArguments;

        try
        {
            var lScoutEmployer = new LEmployer(
                lScoutRunner.LRunnerProgramPath, lScoutRunner.LRunnerArgumentPrefix);
            LEmployerResult lScoutResult = await lScoutEmployer.LEmployerRun(
                lScoutArguments,
                lScoutToken,
                lScoutProcess => lScoutToken.Register(
                    static p => { try { ((Process)p!).Kill(); } catch { } }, lScoutProcess),
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
