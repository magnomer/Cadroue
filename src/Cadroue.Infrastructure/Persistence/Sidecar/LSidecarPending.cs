using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSidecarPending
{
    private static readonly object lSidecarPendingGate = new();
    private static readonly object lSidecarWriteGate = new();
    private static readonly Dictionary<string, List<Action<LSidecarCoreDocument>>> lSidecarPending =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> lSidecarFailure = new(StringComparer.OrdinalIgnoreCase);
    private static Task lSidecarWriter = Task.CompletedTask;

    internal static LSidecarCoreDocument? LSidecarPendingApply(
        string lSidecarSourcePath, LSidecarCoreDocument? lSidecarCore)
    {
        Action<LSidecarCoreDocument>[] lSidecarMutations;
        lock (lSidecarPendingGate)
        {
            if (!lSidecarPending.TryGetValue(LSidecarFullResolve(lSidecarSourcePath), out var lSidecarQueued))
            {
                return lSidecarCore;
            }

            lSidecarMutations = lSidecarQueued.ToArray();
        }

        LSidecarCoreDocument lSidecarOverlay = lSidecarCore ?? new LSidecarCoreDocument();
        foreach (Action<LSidecarCoreDocument> lSidecarMutation in lSidecarMutations)
        {
            lSidecarMutation(lSidecarOverlay);
        }

        return lSidecarOverlay;
    }

    internal static bool LSidecarCoreDefer(string lSidecarSourcePath, Action<LSidecarCoreDocument> lSidecarMutate)
    {
        string lSidecarKey = LSidecarFullResolve(lSidecarSourcePath);
        lock (lSidecarPendingGate)
        {
            if (!lSidecarPending.TryGetValue(lSidecarKey, out var lSidecarQueued))
            {
                lSidecarQueued = new List<Action<LSidecarCoreDocument>>();
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
        Action<LSidecarCoreDocument>[] lSidecarMutations;
        lock (lSidecarPendingGate)
        {
            if (!lSidecarPending.TryGetValue(lSidecarKey, out var lSidecarQueued))
            {
                return true;
            }

            lSidecarMutations = lSidecarQueued.ToArray();
        }

        bool lSidecarSaved = LSidecarCore.LSidecarCoreSave(lSidecarKey, lSidecarCore =>
        {
            foreach (Action<LSidecarCoreDocument> lSidecarMutation in lSidecarMutations)
            {
                lSidecarMutation(lSidecarCore);
            }
        });

        lock (lSidecarPendingGate)
        {
            if (lSidecarPending.TryGetValue(lSidecarKey, out var lSidecarQueued))
            {
                var lSidecarWritten = new HashSet<Action<LSidecarCoreDocument>>(
                    lSidecarMutations, ReferenceEqualityComparer.Instance);
                lSidecarQueued.RemoveAll(lSidecarWritten.Contains);
                if (lSidecarQueued.Count == 0)
                {
                    lSidecarPending.Remove(lSidecarKey);
                }
            }

            if (!lSidecarSaved)
            {
                lSidecarFailure.Add(lSidecarKey);
            }
        }

        if (!lSidecarSaved)
        {
            LTraceLog.LTraceWarningRecord(
                $"Sidecar could not be written for '{Path.GetFileName(lSidecarKey)}'",
                $"{lSidecarMutations.Length} change(s) were not saved to {LSidecarPath.LSidecarPathRead(lSidecarKey)}");
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

        lock (lSidecarPendingGate)
        {
            lSidecarSaved &= lSidecarFailure.Count == 0;
            lSidecarFailure.Clear();
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
