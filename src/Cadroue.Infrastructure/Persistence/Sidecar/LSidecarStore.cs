using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LSidecarStore
{
    public static LSidecar? LSidecarRead(string lSidecarPreciousPath)
    {
        string? lSidecarCoreJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        if (lSidecarCoreJson is null || LSidecarSerializer.LSidecarCoreParse(lSidecarCoreJson) is not { } lSidecarCore)
        {
            return null;
        }

        LSidecarCacheDocument? lSidecarCache = LSidecarCacheStore.LSidecarCacheLoad(
            lSidecarPreciousPath,
            lSidecarCoreJson);
        return LSidecarCreate(lSidecarCore, lSidecarCache);
    }

    public static LSidecar? LSidecarLoad(LKeyframeSourceIdentity lSidecarIdentity)
    {
        LSidecar? lSidecar = LSidecarRead(LSidecarPath.LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath));
        return lSidecar is not null && lSidecar.LSidecarSourceMatch(lSidecarIdentity) ? lSidecar : null;
    }

    public static bool LSidecarSave(
        LKeyframeSourceIdentity lSidecarIdentity,
        IReadOnlyCollection<long> lSidecarKeyframeMilliseconds,
        IReadOnlyCollection<int> lSidecarScannedSpans,
        int lSidecarSpanGridMilliseconds)
    {
        string lSidecarSourcePath = lSidecarIdentity.LKeyframeSourcePath;
        bool lSidecarCoreSaved = LSidecarCore.LSidecarCoreSave(
            lSidecarSourcePath,
            lSidecarCore =>
            {
                lSidecarCore.LSidecarVersion = 2;
                lSidecarCore.LSidecarSource =
                    LSidecarCore.LSidecarSourceCreate(
                        lSidecarIdentity,
                        LSidecarPath.LSidecarPathRead(lSidecarSourcePath));
            });

        bool lSidecarCacheSaved = LSidecarCacheStore.LSidecarCacheSave(
            lSidecarIdentity,
            LSidecarPath.LSidecarPathRead(lSidecarSourcePath),
            lSidecarKeyframeMilliseconds,
            lSidecarScannedSpans,
            lSidecarSpanGridMilliseconds);

        return lSidecarCoreSaved && lSidecarCacheSaved;
    }

    public static IReadOnlyList<long> LSidecarKeyframesRead(string lSidecarSourcePath) =>
        LSidecarRead(LSidecarPath.LSidecarPathRead(lSidecarSourcePath)) is { } lSidecar
        && LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecar.LSidecarSource)
            ? lSidecar.LSidecarKeyframesRead()
            : Array.Empty<long>();

    public static LSidecarSourceResult? LSidecarSourceResolve(string lSidecarPath) =>
        LSidecarRead(lSidecarPath) is { } lSidecar
            ? LSidecarSource.LSidecarSourceResolve(lSidecarPath, lSidecar)
            : null;

    public static bool LSidecarSourceMatch(string lSidecarMediaPath, string lSidecarPath) =>
        LSidecarRead(lSidecarPath) is { } lSidecar
        && LSidecarSource.LSidecarSourceMatch(lSidecarMediaPath, lSidecar.LSidecarSource);

    internal static LSidecar LSidecarCreate(LSidecarCoreDocument lSidecarCore, LSidecarCacheDocument? lSidecarCache)
    {
        LSidecarCacheDocument? lSidecarValid = lSidecarCache is not null
            && LSidecarSource.LSidecarCacheMatch(
                lSidecarCore.LSidecarSource,
                lSidecarCache.LSidecarLength,
                lSidecarCache.LSidecarPartialHash)
                ? LSidecarCacheValidate(lSidecarCache)
                : null;
        return new LSidecar
        {
            LSidecarVersion = lSidecarCore.LSidecarVersion,
            LSidecarSource = lSidecarCore.LSidecarSource,
            LSidecarSections = lSidecarCore.LSidecarSections,
            LSidecarEdit = lSidecarCore.LSidecarEdit,
            LSidecarAudio = lSidecarCore.LSidecarAudio,
            LSidecarKeyframeDeltas = lSidecarValid?.LSidecarKeyframeDeltas ?? new(),
            LSidecarScannedSpans = lSidecarValid?.LSidecarScannedSpans ?? new(),
            LSidecarSpanGrid = lSidecarValid?.LSidecarSpanGrid ?? 0,
            LSidecarWaveform = lSidecarValid?.LSidecarWaveform
        };
    }

    internal static LSidecarCacheDocument? LSidecarCacheValidate(LSidecarCacheDocument? lSidecarCache)
    {
        if (lSidecarCache is not { LSidecarKeyframeCount: > 0 })
        {
            return lSidecarCache;
        }

        IReadOnlyList<long> lSidecarKeyframes = LSidecarKeyframe.LSidecarKeyframeParse(
            lSidecarCache.LSidecarKeyframeDeltas);
        if (LSidecarKeyframe.LSidecarKeyframeCheck(
                lSidecarKeyframes,
                lSidecarCache.LSidecarKeyframeCount,
                lSidecarCache.LSidecarKeyframeLast))
        {
            return lSidecarCache;
        }

        lSidecarCache.LSidecarKeyframeDeltas = new();
        lSidecarCache.LSidecarScannedSpans = new();
        lSidecarCache.LSidecarSpanGrid = 0;
        lSidecarCache.LSidecarKeyframeCount = 0;
        lSidecarCache.LSidecarKeyframeLast = 0;
        return lSidecarCache;
    }
}
