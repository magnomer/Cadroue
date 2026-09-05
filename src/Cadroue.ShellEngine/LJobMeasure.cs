using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
    // Fallback per-input byte sizes for a merge whose sources were not measured at add time.
    private IReadOnlyList<long> LJobMergeRead()
    {
        if (lJobItem.LWorkMergeSources.Count <= 1)
        {
            return Array.Empty<long>();
        }

        var pMergeBytes = new List<long>(lJobItem.LWorkMergeSources.Count);
        foreach (string pMergeSource in lJobItem.LWorkMergeSources)
        {
            pMergeBytes.Add(LScout.LScoutBytesRead(pMergeSource) ?? 0);
        }

        return pMergeBytes;
    }

    // Output figures are stored on the item when the job finishes. The keyframe-interval packet scan
    // runs here only when the video was re-encoded; a stream copy leaves the interval unchanged, so
    // it is inherited from the source. Output loudness is not decoded here: a copied audio stream
    // inherits the source loudness synchronously (no extra decode), and a re-encoded stream is
    // measured off the runner loop by LSubsidiary after the job commits (see LJobRun). This keeps a
    // simple split, which copies both streams, from re-reading each finished output between jobs (a
    // full read that stalls the runner loop on a spinning disk). The source is never measured here:
    // its figures come from the record made when the file was added to the worklist.
    private LWorkMedia? LJobOutputResolve(LWorkMedia? pOutputMedia, string pOutputPath, LWorkMedia? pSourceMedia)
    {
        if (pOutputMedia is null)
        {
            return null;
        }

        bool pVideoEncoded = string.Equals(
            lJobItem.LWorkOutput.LEncodingVideo.LEncodingMode, "Encode", StringComparison.OrdinalIgnoreCase);
        LWorkMedia pMeasured = pOutputMedia;
        if (pMeasured.LWorkMediaVideo && pMeasured.LWorkKeyframeInterval is null)
        {
            double? pInterval = pVideoEncoded
                ? LScout.LScoutIntervalRead(pOutputPath, pMeasured.LWorkMediaDuration, lJobToken)
                : pSourceMedia?.LWorkKeyframeInterval;
            if (pInterval is { } pValue)
            {
                pMeasured = pMeasured with { LWorkKeyframeInterval = pValue };
            }
        }

        if (pMeasured.LWorkMediaSamplerate > 0 && pMeasured.LWorkMediaLoudness is null
            && !lJobItem.LWorkAudio.LWorkAudioActive && pSourceMedia?.LWorkMediaLoudness is { } pInherited)
        {
            pMeasured = pMeasured with { LWorkMediaLoudness = pInherited };
        }

        return pMeasured;
    }
}
