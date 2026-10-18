using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal sealed class LJobMeasure
{
    private readonly LRunner lJobOwner;
    private readonly LWorkItem lJobItem;
    private readonly CancellationToken lJobToken;

    internal LJobMeasure(LRunner lJobRunner, LWorkItem lJobWorkItem, CancellationToken lJobStopToken)
    {
        lJobOwner = lJobRunner;
        lJobItem = lJobWorkItem;
        lJobToken = lJobStopToken;
    }

    internal async Task<double> LJobSecondsRead()
    {
        double pTotalSeconds = lJobItem.LWorkKind switch
        {
            LWorkKind.LWorkKindAudio =>
                (await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken, lJobOwner.LRunnerLevel)
                    .ConfigureAwait(false))
                    ?.LWorkMediaDuration.TotalSeconds ?? 0,
            LWorkKind.LWorkKindMerge =>
                await LScout.LScoutMergeRead(lJobItem.LWorkMergeSources, lJobToken, lJobOwner.LRunnerLevel)
                    .ConfigureAwait(false),
            _ => lJobItem.LWorkDuration.TotalSeconds
        };

        if (pTotalSeconds <= 0 && lJobItem.LWorkKind != LWorkKind.LWorkKindMerge)
        {
            pTotalSeconds = (lJobItem.LWorkSourceMedia
                ?? await LScout.LScoutMediaRead(lJobItem.LWorkSourcePath, lJobToken, lJobOwner.LRunnerLevel)
                    .ConfigureAwait(false))
                ?.LWorkMediaDuration.TotalSeconds ?? 0;
        }

        return pTotalSeconds;
    }

    internal IReadOnlyList<long> LJobMergeRead()
    {
        if (lJobItem.LWorkMergeSources.Count <= 1)
        {
            return Array.Empty<long>();
        }

        var pMergeBytes = new List<long>(lJobItem.LWorkMergeSources.Count);
        foreach (string pMergeSource in lJobItem.LWorkMergeSources)
        {
            pMergeBytes.Add(LScoutBytes.LScoutBytesRead(pMergeSource) ?? 0);
        }

        return pMergeBytes;
    }

    internal async Task<LWorkMedia?> LJobOutputResolve(
        LWorkMedia? pOutputMedia, string pOutputPath, LWorkMedia? pSourceMedia)
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
                ? await LScout.LScoutIntervalRead(pOutputPath, pMeasured, lJobToken).ConfigureAwait(false)
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
