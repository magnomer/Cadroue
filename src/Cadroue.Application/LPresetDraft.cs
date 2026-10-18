using Cadroue.Core;

namespace Cadroue.Application;

public static class LPresetDraft
{
    private static readonly Dictionary<string, LPresetDocument> LPresetDrafts =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly object LPresetDraftLock = new();

    internal static event Action<string>? LPresetDraftChanged;

    internal static event Action<string, string>? LPresetNameChanged;

    public static Func<string, LPresetDocument?>? LPresetLoadSeam;

    internal static void LPresetDraftSync(
        string lPresetName,
        LPresetDocument? lPresetStored,
        LPresetDocument? lPresetPrevious)
    {
        if (lPresetStored is null)
        {
            lock (LPresetDraftLock)
            {
                LPresetDrafts.Remove(lPresetName);
            }

            LPresetNameChanged?.Invoke(lPresetName, LPresetTable.LPresetFirstName ?? string.Empty);
            return;
        }

        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetDocument? lPresetDraft)
                || lPresetPrevious is null
                || !LPresetComparison.LPresetRecordMatch(lPresetPrevious, lPresetDraft))
            {
                return;
            }

            LPresetDrafts[lPresetName] = LPresetDraftCopy(lPresetStored, lPresetName);
        }

        LPresetDraftRaise(lPresetName);
    }

    internal static void LPresetDraftMove(string lOldPresetName, string lNewPresetName)
    {
        lock (LPresetDraftLock)
        {
            if (LPresetDrafts.Remove(lOldPresetName, out LPresetDocument? lPresetDraft))
            {
                lPresetDraft.LPresetName = lNewPresetName;
                LPresetDrafts[lNewPresetName] = lPresetDraft;
            }
        }

        LPresetNameChanged?.Invoke(lOldPresetName, lNewPresetName);
    }

    internal static LPresetDocument LPresetDraftPrepare(
        string lOldName,
        string lName,
        out LPresetDocument? lPresetPrior)
    {
        lock (LPresetDraftLock)
        {
            LPresetDocument lRecord = LPresetDraftRead(lOldName);
            LPresetDrafts.TryGetValue(lName, out lPresetPrior);
            lRecord.LPresetName = lName;
            LPresetDrafts[lName] = lRecord;
            return lRecord;
        }
    }

    internal static void LPresetDraftCancel(
        string lOldName,
        LPresetDocument lRecord,
        string lName,
        LPresetDocument? lPresetPrior)
    {
        lRecord.LPresetName = lOldName;
        lock (LPresetDraftLock)
        {
            LPresetDrafts[lOldName] = lRecord;
            if (!string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase))
            {
                LPresetDraftRestore(lName, lPresetPrior);
            }
        }
    }

    internal static void LPresetDraftCommit(string lOldName, string lName)
    {
        if (!string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase))
        {
            lock (LPresetDraftLock)
            {
                LPresetDrafts.Remove(lOldName);
            }

            LPresetDraftReset(lOldName);
        }

        LPresetDraftRaise(lName);
    }

    internal static void LPresetDraftAccept(string lPresetName, LPresetDocument lPresetValue)
    {
        lPresetValue.LPresetName = lPresetName;
        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetDocument? lPresetDraft)
                || (LPresetLoadSeam?.Invoke(lPresetName) is { } lPresetStored
                    && LPresetComparison.LPresetRecordMatch(lPresetStored, lPresetDraft)))
            {
                LPresetDrafts[lPresetName] = lPresetValue;
            }
        }
    }

    internal static LPresetDocument LPresetDraftRead(string lPresetName)
    {
        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetDocument? lPresetDraft))
            {
                lPresetDraft = LPresetDraftCreate(lPresetName);
                LPresetDrafts[lPresetName] = lPresetDraft;
            }

            return lPresetDraft;
        }
    }

    private static LPresetDocument LPresetDraftCreate(string lPresetName)
    {
        LPresetDocument lPresetDraft = LPresetLoadSeam?.Invoke(lPresetName) ?? new LPresetDocument();
        lPresetDraft.LPresetName = lPresetName;
        return lPresetDraft;
    }

    internal static void LPresetDraftSet(string lPresetName, LPresetDocument lPresetValue)
    {
        lPresetValue.LPresetName = lPresetName;
        lock (LPresetDraftLock)
        {
            LPresetDrafts[lPresetName] = lPresetValue;
        }

        LPresetDraftRaise(lPresetName);
    }

    private static void LPresetDraftRestore(string lPresetName, LPresetDocument? lPresetPrior)
    {
        lock (LPresetDraftLock)
        {
            if (lPresetPrior is null)
            {
                LPresetDrafts.Remove(lPresetName);
                return;
            }

            LPresetDrafts[lPresetName] = lPresetPrior;
        }
    }

    internal static void LPresetDraftReset(string lPresetName)
    {
        if (LPresetLoadSeam?.Invoke(lPresetName) is not { } lPresetStored)
        {
            return;
        }

        LPresetDocument lPresetDraft = LPresetDraftCopy(lPresetStored, lPresetName);
        lock (LPresetDraftLock)
        {
            LPresetDrafts[lPresetName] = lPresetDraft;
        }

        LPresetDraftRaise(lPresetName);
    }

    private static LPresetDocument LPresetDraftCopy(LPresetDocument lPresetRecord, string lPresetName)
    {
        LPresetDocument lPresetDraft = LPreset.LPresetStateCreate(lPresetRecord).LPresetRecordCreate();
        lPresetDraft.LPresetName = lPresetName;
        return lPresetDraft;
    }

    private static void LPresetDraftRaise(string lPresetName) => LPresetDraftChanged?.Invoke(lPresetName);

    internal static void LPresetDraftAttach(string lPresetName)
    {
        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.ContainsKey(lPresetName))
            {
                LPresetDrafts[lPresetName] = LPresetDraftCreate(lPresetName);
            }
        }
    }
}
