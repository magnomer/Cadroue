using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

internal static class LSidecarCacheStore
{
    internal static LSidecarCacheDocument? LSidecarCacheRead(string lSidecarSourcePath) =>
        LSidecarCacheLoad(LSidecarStore.LSidecarPathRead(lSidecarSourcePath), null);

    internal static bool LSidecarDiagnosisSave(
        string lSidecarSourcePath,
        LKeyframeSourceIdentity lSidecarIdentity,
        IReadOnlyCollection<LSidecarDossier> lSidecarDossiers) =>
        LSidecarCacheChange(lSidecarSourcePath, lSidecarIdentity, lSidecarCache =>
            lSidecarCache.LSidecarDiagnosis = new LSidecarDiagnosisEntry
            {
                LSidecarLength = lSidecarIdentity.LKeyframeSourceLength,
                LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash,
                LSidecarDossiers = lSidecarDossiers.ToList(),
                LSidecarScannedKinds = Enum.GetValues<LFlawKind>().ToList()
            });

    internal static IReadOnlyList<LSidecarDossier>? LSidecarDiagnosisRead(string lSidecarSourcePath)
    {
        if (LSidecarCacheRead(lSidecarSourcePath) is not { LSidecarDiagnosis: { } lSidecarDiagnosis })
        {
            return null;
        }

        if (lSidecarDiagnosis.LSidecarScannedKinds.Count == 0)
        {
            return null;
        }

        var lSidecarFingerprint = new LSidecarSourceEntry
        {
            LSidecarLength = lSidecarDiagnosis.LSidecarLength,
            LSidecarPartialHash = lSidecarDiagnosis.LSidecarPartialHash
        };
        return LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecarFingerprint)
            ? lSidecarDiagnosis.LSidecarDossiers
            : null;
    }

    internal static LSidecarCacheDocument? LSidecarCacheLoad(string lSidecarPreciousPath, string? lSidecarPreciousJson)
    {
        string lSidecarCachePath = LSidecarStore.LSidecarCacheResolve(lSidecarPreciousPath);
        string? lSidecarCacheJson = LSidecarFile.LSidecarFileRead(lSidecarCachePath);
        if (lSidecarCacheJson is not null)
        {
            return LSidecarSerializer.LSidecarCacheParse(lSidecarCacheJson);
        }

        string? lSidecarLegacyJson = lSidecarPreciousJson ?? LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
        return lSidecarLegacyJson is null ? null : LSidecarSerializer.LSidecarCacheParse(lSidecarLegacyJson);
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

        LSidecarCacheDocument? lSidecarLegacy = LSidecarSerializer.LSidecarCacheParse(lSidecarLegacyJson);
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
                LSidecarFile.LSidecarFileSave(
                    lSidecarCachePath, LSidecarSerializer.LSidecarCacheFormat(lSidecarLegacy));
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
                LSidecarCacheDocument lSidecarCache = LSidecarBaseRead(
                    lSidecarCachePath,
                    lSidecarPreciousPath,
                    lSidecarIdentity);

                List<long> lSidecarDeltas = LSidecarKeyframe.LSidecarKeyframeFormat(lSidecarKeyframeMilliseconds);
                IReadOnlyList<long> lSidecarKeyframes = LSidecarKeyframe.LSidecarKeyframeParse(lSidecarDeltas);
                lSidecarCache.LSidecarKeyframeDeltas = lSidecarDeltas;
                lSidecarCache.LSidecarKeyframeCount = lSidecarKeyframes.Count;
                lSidecarCache.LSidecarKeyframeLast = LSidecarKeyframe.LSidecarLastRead(lSidecarKeyframes);
                lSidecarCache.LSidecarScannedSpans = lSidecarScannedSpans
                    .Where(lSpan => lSpan >= 0)
                    .Distinct()
                    .Order()
                    .ToList();
                lSidecarCache.LSidecarSpanGrid = lSidecarSpanGridMilliseconds;

                return LSidecarFile.LSidecarFileSave(
                    lSidecarCachePath,
                    LSidecarSerializer.LSidecarCacheFormat(lSidecarCache));
            }
        }
        catch (Exception lException) when (lException is IOException or UnauthorizedAccessException or TimeoutException)
        {
            return false;
        }
    }

    internal static bool LSidecarCacheChange(string lSidecarSourcePath, Action<LSidecarCacheDocument> lSidecarChange)
    {
        try
        {
            return LSidecarCacheChange(
                lSidecarSourcePath,
                LKeyframeSourceIdentity.LKeyframeIdentityCreate(lSidecarSourcePath, TimeSpan.Zero),
                lSidecarChange);
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return false;
        }
    }

    internal static bool LSidecarCacheChange(
        string lSidecarSourcePath,
        LKeyframeSourceIdentity lSidecarIdentity,
        Action<LSidecarCacheDocument> lSidecarChange)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarStore.LSidecarPathRead(lSidecarSourcePath);
            string lSidecarCachePath = LSidecarStore.LSidecarCacheResolve(lSidecarPreciousPath);
            using (LLatch.LLatchClaim(lSidecarCachePath))
            {
                LSidecarCacheDocument lSidecarCache = LSidecarBaseRead(
                    lSidecarCachePath,
                    lSidecarPreciousPath,
                    lSidecarIdentity);
                lSidecarChange(lSidecarCache);
                return LSidecarFile.LSidecarFileSave(
                    lSidecarCachePath,
                    LSidecarSerializer.LSidecarCacheFormat(lSidecarCache));
            }
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or TimeoutException)
        {
            return false;
        }
    }

    private static LSidecarCacheDocument LSidecarBaseRead(
        string lSidecarCachePath,
        string lSidecarPreciousPath,
        LKeyframeSourceIdentity lSidecarIdentity)
    {
        LSidecarCacheDocument? lSidecarStored = null;
        string? lSidecarCacheJson = LSidecarFile.LSidecarFileRead(lSidecarCachePath);
        if (lSidecarCacheJson is not null
            && LSidecarSerializer.LSidecarCacheParse(lSidecarCacheJson) is { } lSidecarCache)
        {
            lSidecarStored = lSidecarCache;
        }
        else
        {
            string? lSidecarLegacyJson = LSidecarFile.LSidecarFileRead(lSidecarPreciousPath);
            if (lSidecarLegacyJson is not null
                && LSidecarSerializer.LSidecarCacheParse(lSidecarLegacyJson) is { } lSidecarLegacy
                && (lSidecarLegacy.LSidecarKeyframeDeltas.Count > 0 || lSidecarLegacy.LSidecarWaveform is not null))
            {
                LSidecarCacheApply(lSidecarLegacy);
                lSidecarStored = lSidecarLegacy;
            }
        }

        bool lSidecarOwned = lSidecarStored is not null
            && lSidecarStored.LSidecarLength == lSidecarIdentity.LKeyframeSourceLength
            && string.Equals(
                lSidecarStored.LSidecarPartialHash,
                lSidecarIdentity.LKeyframePartialHash,
                StringComparison.Ordinal);
        LSidecarCacheDocument lSidecarBase = lSidecarOwned ? lSidecarStored! : new LSidecarCacheDocument();
        lSidecarBase.LSidecarVersion = 2;
        lSidecarBase.LSidecarLength = lSidecarIdentity.LKeyframeSourceLength;
        lSidecarBase.LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash;
        return lSidecarBase;
    }

    private static void LSidecarCacheApply(LSidecarCacheDocument lSidecarCache)
    {
        IReadOnlyList<long> lSidecarKeyframes = LSidecarKeyframe.LSidecarKeyframeParse(
            lSidecarCache.LSidecarKeyframeDeltas);
        lSidecarCache.LSidecarVersion = 2;
        lSidecarCache.LSidecarKeyframeCount = lSidecarKeyframes.Count;
        lSidecarCache.LSidecarKeyframeLast = LSidecarKeyframe.LSidecarLastRead(lSidecarKeyframes);
    }
}
