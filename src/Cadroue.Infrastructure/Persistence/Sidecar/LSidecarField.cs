using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static class LSidecarField
{
    public static Func<string, LSidecarCoreDocument?> LSidecarCoreSeam { get; set; } = _ => null;

    public static LSidecarCoreDocument? LSidecarCoreRead(string lSidecarSourcePath)
    {
        LSidecarCoreResult lSidecarResult =
            LSidecarCore.LSidecarCoreResolve(lSidecarSourcePath, LSidecarPath.LSidecarPathRead(lSidecarSourcePath));
        LSidecarCoreDocument? lSidecarCore = lSidecarResult.LSidecarCoreState == LSidecarReadKind.LSidecarReadMatched
            ? lSidecarResult.LSidecarCoreValue
            : null;
        return LSidecarPending.LSidecarPendingApply(lSidecarSourcePath, lSidecarCore);
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
            if (LSidecarCacheStore.LSidecarCacheRead(lSidecarSourcePath)
                is not { LSidecarDiagnosis: { } lSidecarDiagnosis })
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
        LSidecarCacheStore.LSidecarCacheChange(lSidecarSourcePath, lSidecarIdentity, lSidecarCache =>
            lSidecarCache.LSidecarDiagnosis = new LSidecarDiagnosisEntry
            {
                LSidecarLength = lSidecarIdentity.LKeyframeSourceLength,
                LSidecarPartialHash = lSidecarIdentity.LKeyframePartialHash,
                LSidecarDossiers = lSidecarDossiers.ToList(),
                LSidecarScannedKinds = Enum.GetValues<LFlawKind>().ToList()
            });
}
