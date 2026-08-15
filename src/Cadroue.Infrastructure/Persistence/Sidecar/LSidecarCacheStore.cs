using Cadroue.Core;

namespace Cadroue.Infrastructure;

internal static class LSidecarCacheStore
{
    internal static LSidecarCacheRecord? LSidecarCacheRead(string lSidecarSourcePath) =>
        LSidecarCacheLoad(LSidecarStore.LSidecarPathRead(lSidecarSourcePath), null);

    internal static LSidecarCacheRecord? LSidecarCacheLoad(string lSidecarPreciousPath, string? lSidecarPreciousJson)
    {
        string lSidecarCachePath = LSidecarStore.LSidecarCacheResolve(lSidecarPreciousPath);
        string? lSidecarCacheJson = LSidecarFile.LSidecarFileRead(lSidecarCachePath);
        if (lSidecarCacheJson is not null)
        {
            return LSidecarParse.LSidecarCacheParse(lSidecarCacheJson);
        }

        string? lSidecarLegacyJson = lSidecarPreciousJson ?? LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        return lSidecarLegacyJson is null ? null : LSidecarParse.LSidecarCacheParse(lSidecarLegacyJson);
    }

    internal static void LSidecarCacheMove(string lSidecarPreciousPath, string? lSidecarLegacyJson)
    {
        if (lSidecarLegacyJson is null)
        {
            return;
        }

        string lSidecarCachePath = LSidecarStore.LSidecarCacheResolve(lSidecarPreciousPath);
        if (File.Exists(lSidecarCachePath))
        {
            return;
        }

        LSidecarCacheRecord? lSidecarLegacy = LSidecarParse.LSidecarCacheParse(lSidecarLegacyJson);
        if (lSidecarLegacy is null
            || (lSidecarLegacy.LSidecarKeyframeDeltas.Count == 0 && lSidecarLegacy.LSidecarWaveform is null))
        {
            return;
        }

        try
        {
            using (LLatch.LLatchClaim(lSidecarCachePath))
            {
                if (File.Exists(lSidecarCachePath))
                {
                    return;
                }

                LSidecarCacheApply(lSidecarLegacy);
                LSidecarFile.LSidecarFileSave(lSidecarCachePath, LSidecarParse.LSidecarCacheFormat(lSidecarLegacy));
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or TimeoutException)
        {
        }
    }

    internal static bool LSidecarCacheSave(
        LKeyframeSourceIdentity lSidecarIdentity,
        string lSidecarPreciousPath,
        IReadOnlyCollection<long> lSidecarKeyframeMilliseconds,
        IReadOnlyCollection<int> lSidecarScannedSpans,
        int lSidecarSpanGridMilliseconds)
    {
        string lSidecarCachePath = LSidecarStore.LSidecarCacheResolve(lSidecarPreciousPath);
        try
        {
            using (LLatch.LLatchClaim(lSidecarCachePath))
            {
                LSidecarCacheRecord lSidecarCache = LSidecarBaseRead(lSidecarCachePath, lSidecarPreciousPath);
                lSidecarCache.LSidecarVersion = 2;
                lSidecarCache.LSidecarLength = lSidecarIdentity.LKeyframeSourceLength;
                lSidecarCache.LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash;

                List<long> lSidecarDeltas = LSidecarKeyframe.LSidecarKeyframeFormat(lSidecarKeyframeMilliseconds);
                IReadOnlyList<long> lSidecarKeyframes = LSidecarKeyframe.LSidecarKeyframeParse(lSidecarDeltas);
                lSidecarCache.LSidecarKeyframeDeltas = lSidecarDeltas;
                lSidecarCache.LSidecarKeyframeCount = lSidecarKeyframes.Count;
                lSidecarCache.LSidecarKeyframeLast = LSidecarKeyframe.LSidecarLastRead(lSidecarKeyframes);
                lSidecarCache.LSidecarScannedSpans = lSidecarScannedSpans.Where(lSpan => lSpan >= 0).Distinct().Order().ToList();
                lSidecarCache.LSidecarSpanGrid = lSidecarSpanGridMilliseconds;

                return LSidecarFile.LSidecarFileSave(lSidecarCachePath, LSidecarParse.LSidecarCacheFormat(lSidecarCache));
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or TimeoutException)
        {
            return false;
        }
    }

    internal static bool LSidecarCacheChange(string lSidecarSourcePath, Action<LSidecarCacheRecord> lSidecarChange)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarStore.LSidecarPathRead(lSidecarSourcePath);
            string lSidecarCachePath = LSidecarStore.LSidecarCacheResolve(lSidecarPreciousPath);
            using (LLatch.LLatchClaim(lSidecarCachePath))
            {
                LSidecarCacheRecord lSidecarCache = LSidecarBaseRead(lSidecarCachePath, lSidecarPreciousPath);
                lSidecarChange(lSidecarCache);
                return LSidecarFile.LSidecarFileSave(lSidecarCachePath, LSidecarParse.LSidecarCacheFormat(lSidecarCache));
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or ArgumentException or TimeoutException)
        {
            return false;
        }
    }

    private static LSidecarCacheRecord LSidecarBaseRead(string lSidecarCachePath, string lSidecarPreciousPath)
    {
        string? lSidecarCacheJson = LSidecarFile.LSidecarFileRead(lSidecarCachePath);
        if (lSidecarCacheJson is not null && LSidecarParse.LSidecarCacheParse(lSidecarCacheJson) is { } lSidecarCache)
        {
            return lSidecarCache;
        }

        string? lSidecarLegacyJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        if (lSidecarLegacyJson is not null && LSidecarParse.LSidecarCacheParse(lSidecarLegacyJson) is { } lSidecarLegacy
            && (lSidecarLegacy.LSidecarKeyframeDeltas.Count > 0 || lSidecarLegacy.LSidecarWaveform is not null))
        {
            LSidecarCacheApply(lSidecarLegacy);
            return lSidecarLegacy;
        }

        return new LSidecarCacheRecord();
    }

    private static void LSidecarCacheApply(LSidecarCacheRecord lSidecarCache)
    {
        IReadOnlyList<long> lSidecarKeyframes = LSidecarKeyframe.LSidecarKeyframeParse(lSidecarCache.LSidecarKeyframeDeltas);
        lSidecarCache.LSidecarVersion = 2;
        lSidecarCache.LSidecarKeyframeCount = lSidecarKeyframes.Count;
        lSidecarCache.LSidecarKeyframeLast = LSidecarKeyframe.LSidecarLastRead(lSidecarKeyframes);
    }
}
