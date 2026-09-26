using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.Infrastructure;

public static partial class LSidecarStore
{
    private static readonly object lSidecarPendingGate = new();
    private static readonly object lSidecarWriteGate = new();
    private static readonly Dictionary<string, List<Action<LSidecarCoreRecord>>> lSidecarPending =
        new(StringComparer.OrdinalIgnoreCase);
    private static Task lSidecarWriter = Task.CompletedTask;

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
        LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarEdit = lSidecarEdit);

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
        LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarAudio = lSidecarAudio);

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
        LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarSplit = lSidecarSplit);

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
        LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarFix = lSidecarFix);

    public static LSidecarWaveformRecord? LSidecarWaveformRead(string lSidecarSourcePath)
    {
        try
        {
            return LSidecarCacheStore.LSidecarCacheRead(lSidecarSourcePath) is { } lSidecarCache
                && LSidecarSource.LSidecarSourceMatch(
                    lSidecarSourcePath,
                    new LSidecarSourceRecord
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
        LSidecarCoreDefer(lSidecarSourcePath, lSidecarCore => lSidecarCore.LSidecarLoudness = lSidecarLoudness);

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
            string lSidecarPreciousPath = LSidecarPathRead(lSidecarSourcePath);
            LSidecarCoreRecord? lSidecarCore;
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

            LSidecarCoreSave(
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

    private static LSidecarCoreRecord? LSidecarPendingApply(string lSidecarSourcePath, LSidecarCoreRecord? lSidecarCore)
    {
        Action<LSidecarCoreRecord>[] lSidecarMutations;
        lock (lSidecarPendingGate)
        {
            if (!lSidecarPending.TryGetValue(LSidecarFullResolve(lSidecarSourcePath), out var lSidecarQueued))
            {
                return lSidecarCore;
            }

            lSidecarMutations = lSidecarQueued.ToArray();
        }

        LSidecarCoreRecord lSidecarOverlay = lSidecarCore ?? new LSidecarCoreRecord();
        foreach (Action<LSidecarCoreRecord> lSidecarMutation in lSidecarMutations)
        {
            lSidecarMutation(lSidecarOverlay);
        }

        return lSidecarOverlay;
    }

    private static bool LSidecarCoreDefer(string lSidecarSourcePath, Action<LSidecarCoreRecord> lSidecarMutate)
    {
        string lSidecarKey = LSidecarFullResolve(lSidecarSourcePath);
        lock (lSidecarPendingGate)
        {
            if (!lSidecarPending.TryGetValue(lSidecarKey, out var lSidecarQueued))
            {
                lSidecarQueued = new List<Action<LSidecarCoreRecord>>();
                lSidecarPending[lSidecarKey] = lSidecarQueued;
            }

            lSidecarQueued.Add(lSidecarMutate);
            lSidecarWriter = lSidecarWriter.ContinueWith(
                _ => { LSidecarPendingPersist(lSidecarKey); },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }

        return true;
    }

    private static bool LSidecarPendingPersist(string lSidecarKey)
    {
        lock (lSidecarWriteGate)
        {
            return LSidecarPendingSave(lSidecarKey);
        }
    }

    private static bool LSidecarPendingSave(string lSidecarKey)
    {
        Action<LSidecarCoreRecord>[] lSidecarMutations;
        lock (lSidecarPendingGate)
        {
            if (!lSidecarPending.TryGetValue(lSidecarKey, out var lSidecarQueued))
            {
                return true;
            }

            lSidecarMutations = lSidecarQueued.ToArray();
        }

        bool lSidecarSaved = LSidecarCoreSave(lSidecarKey, lSidecarCore =>
        {
            foreach (Action<LSidecarCoreRecord> lSidecarMutation in lSidecarMutations)
            {
                lSidecarMutation(lSidecarCore);
            }
        });

        lock (lSidecarPendingGate)
        {
            if (lSidecarPending.TryGetValue(lSidecarKey, out var lSidecarQueued))
            {
                var lSidecarWritten = new HashSet<Action<LSidecarCoreRecord>>(
                    lSidecarMutations, ReferenceEqualityComparer.Instance);
                lSidecarQueued.RemoveAll(lSidecarWritten.Contains);
                if (lSidecarQueued.Count == 0)
                {
                    lSidecarPending.Remove(lSidecarKey);
                }
            }
        }

        if (!lSidecarSaved)
        {
            LTraceLog.LTraceWarningRecord(
                $"Sidecar could not be written for '{Path.GetFileName(lSidecarKey)}'",
                $"{lSidecarMutations.Length} change(s) were not saved to {LSidecarPathRead(lSidecarKey)}");
        }

        return lSidecarSaved;
    }

    public static bool LSidecarPersist()
    {
        string[] lSidecarKeys;
        lock (lSidecarPendingGate)
        {
            lSidecarKeys = lSidecarPending.Keys.ToArray();
        }

        bool lSidecarSaved = true;
        foreach (string lSidecarKey in lSidecarKeys)
        {
            lSidecarSaved &= LSidecarPendingPersist(lSidecarKey);
        }

        return lSidecarSaved;
    }

    private static string LSidecarFullResolve(string lSidecarSourcePath)
    {
        try
        {
            return Path.GetFullPath(lSidecarSourcePath);
        }
        catch (Exception lException) when (lException is ArgumentException or NotSupportedException or IOException)
        {
            return lSidecarSourcePath;
        }
    }
}
