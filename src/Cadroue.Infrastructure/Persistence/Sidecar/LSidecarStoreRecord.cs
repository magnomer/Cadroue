using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static partial class LSidecarStore
{
    public static LSidecarEditRecord? LSidecarEditRead(string lSidecarSourcePath)
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

    public static bool LSidecarEditSave(string lSidecarSourcePath, LSidecarEditRecord? lSidecarEdit) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarEdit = lSidecarEdit);

    public static LSidecarAudioRecord? LSidecarAudioRead(string lSidecarSourcePath)
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

    public static bool LSidecarAudioSave(string lSidecarSourcePath, LSidecarAudioRecord? lSidecarAudio) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarAudio = lSidecarAudio);

    public static LSidecarSplitRecord? LSidecarSplitRead(string lSidecarSourcePath)
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

    public static bool LSidecarSplitSave(string lSidecarSourcePath, LSidecarSplitRecord? lSidecarSplit) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarSplit = lSidecarSplit);

    public static LSidecarFixRecord? LSidecarFixRead(string lSidecarSourcePath)
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

    public static bool LSidecarFixSave(string lSidecarSourcePath, LSidecarFixRecord? lSidecarFix) =>
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarFix = lSidecarFix);

    public static LSidecarWaveformRecord? LSidecarWaveformRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCacheStore.LSidecarCacheRead(lSidecarSourcePath)?.LSidecarWaveform;
        }
        catch (Exception lException) when (
            lException is IOException
                or UnauthorizedAccessException
                or ArgumentException)
        {
            return null;
        }
    }

    public static bool LSidecarWaveformSave(string lSidecarSourcePath, LSidecarWaveformRecord? lSidecarWaveform) =>
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
        LSidecarCoreSave(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarLoudness = lSidecarLoudness);

    public static TimeSpan LSidecarDurationRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCoreRead(lSidecarSourcePath)
                    is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarCore
                && LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecarCore.LSidecarSource)
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

    public static TimeSpan LSidecarDurationResolve(string lSidecarSourcePath)
    {
        try
        {
            string lSidecarPreciousPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecarCoreRecord? lSidecarCore;
            using (LLatch.LLatchClaim(lSidecarPreciousPath))
            {
                lSidecarCore = LSidecarCoreRead(lSidecarSourcePath);
            }

            if (lSidecarCore is { LSidecarSource.LSidecarDurationMilliseconds: > 0 } lSidecarKnown
                && LSidecarSource.LSidecarSourceMatch(lSidecarSourcePath, lSidecarKnown.LSidecarSource))
            {
                return TimeSpan.FromMilliseconds(lSidecarKnown.LSidecarSource.LSidecarDurationMilliseconds);
            }

            TimeSpan lSidecarProbed;
            try
            {
                lSidecarProbed = LMedia.LMediaFfprobeRead(lSidecarSourcePath).LMediaInfoDuration;
            }
            catch (Exception)
            {
                return TimeSpan.Zero;
            }

            if (lSidecarProbed <= TimeSpan.Zero)
            {
                return TimeSpan.Zero;
            }

            LSidecarCoreSave(lSidecarSourcePath, lSidecarTarget =>
            {
                try
                {
                    lSidecarTarget.LSidecarSource = LSidecarSourceCreate(
                        LKeyframeSourceIdentity.LKeyframeIdentityCreate(lSidecarSourcePath, lSidecarProbed),
                        lSidecarPreciousPath);
                }
                catch (Exception lSidecarException) when (
                    lSidecarException is IOException
                        or UnauthorizedAccessException
                        or ArgumentException
                        or FileNotFoundException)
                {
                    lSidecarTarget.LSidecarSource.LSidecarDurationMilliseconds =
                        (long)Math.Round(lSidecarProbed.TotalMilliseconds);
                }
            });

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
