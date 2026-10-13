using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LSidecarStore
{
    public static Func<string, LSidecarCoreDocument?> LSidecarCoreSeam { get; set; } = _ => null;

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
        return LSidecarSerializer.LSidecarCreate(lSidecarCore, lSidecarCache);
    }

    public static LSidecar? LSidecarLoad(LKeyframeSourceIdentity lSidecarIdentity)
    {
        LSidecar? lSidecar = LSidecarRead(LSidecarPath.LSidecarPathRead(lSidecarIdentity.LKeyframeSourcePath));
        return lSidecar is not null && lSidecar.LSidecarSourceMatch(lSidecarIdentity) ? lSidecar : null;
    }

    public static bool LSidecarSectionsSave(
        string lSidecarSourcePath,
        IReadOnlyList<LSidecarSectionEntry> lSidecarSections)
    {
        List<LSidecarSectionEntry> lSidecarCopy = lSidecarSections.ToList();
        return LSidecarPending.LSidecarCoreDefer(
            lSidecarSourcePath,
            lSidecarCore => lSidecarCore.LSidecarSections = lSidecarCopy);
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

    public static LSidecarCoreDocument? LSidecarCoreRead(string lSidecarSourcePath)
    {
        LSidecarCoreResult lSidecarResult =
            LSidecarCore.LSidecarCoreResolve(lSidecarSourcePath, LSidecarPath.LSidecarPathRead(lSidecarSourcePath));
        LSidecarCoreDocument? lSidecarCore = lSidecarResult.LSidecarCoreState == LSidecarReadKind.LSidecarReadMatched
            ? lSidecarResult.LSidecarCoreValue
            : null;
        return LSidecarPending.LSidecarPendingApply(lSidecarSourcePath, lSidecarCore);
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

    public static IReadOnlyList<LSidecarSectionEntry> LSidecarSectionsRead(string lSidecarSourcePath)
    {
        try
        {
            if (LSidecarCoreSeam(lSidecarSourcePath) is { } lSidecarCore)
            {
                return lSidecarCore.LSidecarSections;
            }
        }
        catch (Exception lSidecarException)
        {
            LTraceLog.LTraceErrorRecord("Sidecar sections could not be restored", lSidecarException);
        }

        return Array.Empty<LSidecarSectionEntry>();
    }

    public static LSidecarEditEntry? LSidecarEditRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarEdit;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarEditSave(string lSidecarSourcePath, LSidecarEditEntry? lSidecarEdit) =>
        LSidecarPending.LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarEdit = lSidecarEdit);

    public static LSidecarAudioEntry? LSidecarAudioRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarAudio;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarAudioSave(string lSidecarSourcePath, LSidecarAudioEntry? lSidecarAudio) =>
        LSidecarPending.LSidecarCoreDefer(
            lSidecarSourcePath,
            lSidecarCore => lSidecarCore.LSidecarAudio = lSidecarAudio);

    public static LSidecarSplitEntry? LSidecarSplitRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarSplit;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarSplitSave(string lSidecarSourcePath, LSidecarSplitEntry? lSidecarSplit) =>
        LSidecarPending.LSidecarCoreDefer(
            lSidecarSourcePath,
            lSidecarCore => lSidecarCore.LSidecarSplit = lSidecarSplit);

    public static LSidecarFixEntry? LSidecarFixRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarFix;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarFixSave(string lSidecarSourcePath, LSidecarFixEntry? lSidecarFix) =>
        LSidecarPending.LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarFix = lSidecarFix);

    public static LSidecarWaveformEntry? LSidecarWaveformRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCacheStore.LSidecarCacheRead(lSidecarSourcePath) is { } lSidecarCache
                && LSidecarSource.LSidecarSourceMatch(
                    lSidecarSourcePath,
                    new LSidecarSourceEntry
                    {
                        LSidecarLength = lSidecarCache.LSidecarLength,
                        LSidecarPartialHash = lSidecarCache.LSidecarPartialHash
                    })
                ? lSidecarCache.LSidecarWaveform
                : null;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarWaveformSave(string lSidecarSourcePath, LSidecarWaveformEntry? lSidecarWaveform) =>
        LSidecarCacheStore.LSidecarCacheChange(
            lSidecarSourcePath,
            lSidecarCache => lSidecarCache.LSidecarWaveform = lSidecarWaveform);

    public static IReadOnlyList<LSidecarDossier>? LSidecarDiagnosisRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCacheStore.LSidecarDiagnosisRead(lSidecarSourcePath);
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarDiagnosisSave(
        string lSidecarSourcePath,
        LKeyframeSourceIdentity lSidecarIdentity,
        IReadOnlyCollection<LSidecarDossier> lSidecarDossiers) =>
        LSidecarCacheStore.LSidecarDiagnosisSave(lSidecarSourcePath, lSidecarIdentity, lSidecarDossiers);

    public static double LSidecarLoudnessRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)?.LSidecarLoudness ?? 0;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return 0;
        }
    }

    public static bool LSidecarLoudnessSave(string lSidecarSourcePath, double lSidecarLoudness) =>
        LSidecarPending.LSidecarCoreDefer(
            lSidecarSourcePath,
            lSidecarCore => lSidecarCore.LSidecarLoudness = lSidecarLoudness);

    public static TimeSpan LSidecarDurationRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)
                    is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarCore
                ? TimeSpan.FromMilliseconds(lSidecarCore.LSidecarSource.LSidecarDurationMilliseconds)
                : TimeSpan.Zero;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return TimeSpan.Zero;
        }
    }

    public static async Task<TimeSpan> LSidecarDurationResolve(string lSidecarSourcePath)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarPath.LSidecarPathRead(lSidecarSourcePath);
            LSidecarCoreDocument? lSidecarCore;
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                lSidecarCore = LSidecarCoreRead(lSidecarSourcePath);
            }

            if (lSidecarCore is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarKnown)
            {
                return TimeSpan.FromMilliseconds(lSidecarKnown.LSidecarSource.LSidecarDurationMilliseconds);
            }

            TimeSpan lSidecarProbed;
            try
            {
                lSidecarProbed = (await LMedia.LMediaFfprobeRead(lSidecarSourcePath).ConfigureAwait(false))
                    .LMediaInfoDuration;
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }

            if (lSidecarProbed <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            LSidecarCore.LSidecarCoreSave(
                lSidecarSourcePath,
                lSidecarTarget => lSidecarTarget.LSidecarSource.LSidecarDurationMilliseconds =
                    (long)Math.Round(lSidecarProbed.TotalMilliseconds));

            return lSidecarProbed;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or TimeoutException)
        {
            return TimeSpan.Zero;
        }
    }
}
