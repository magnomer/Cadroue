using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed partial class LJob
{
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
