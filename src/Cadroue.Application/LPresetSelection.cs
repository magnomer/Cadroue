using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LPresetSelection
{
    private static readonly Dictionary<string, LPresetDocument> LPresetDrafts =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly object LPresetDraftLock = new();

    private static event Action<string>? LPresetDraftChanged;

    private static event Action<string, string>? LPresetNameChanged;

    public static Func<string, LPresetDocument?>? LPresetLoadSeam;
    public static Func<string, LPresetDocument, bool>? LPresetSaveSeam;
    public static Func<string, string, LPresetDocument, bool>? LPresetRenameSeam;
    public static Func<LPresetDocument, LEncoding>? LPresetOutputSeam;
    public static Func<string, bool>? LPresetNativeSeam;

    public LPresetSelection(string lPresetName)
    {
        LPresetSelectionName = LPresetSelectionResolve(lPresetName);
        LPresetDraftAttach(LPresetSelectionName);
        LPresetDraftChanged += LPresetDraftHandle;
        LPresetNameChanged += LPresetNameHandle;
    }

    public LPresetSelection(LPresetDocument lPresetValue, string lPresetName)
        : this(lPresetName)
    {
        if (string.Equals(LPresetSelectionName, lPresetName, StringComparison.OrdinalIgnoreCase))
        {
            LPresetDraftAccept(lPresetName, lPresetValue);
        }
    }

    public event Action? LPresetSelectionChanged;

    public string LPresetSelectionName { get; private set; }

    public LPresetDocument LPresetSelectionValue
    {
        get => LPresetDraftRead(LPresetSelectionName);
        set
        {
            if (LPresetNameCheck(value.LPresetName))
            {
                LPresetSelectionName = value.LPresetName;
            }

            LPresetDraftSet(LPresetSelectionName, value);
        }
    }

    public bool LPresetSelectionValid => LPresetNameCheck(LPresetSelectionName);

    public LEncoding? LPresetSelectionEncoding =>
        LPresetSelectionValid ? LPresetOutputSeam?.Invoke(LPresetSelectionValue) : null;

    public void LPresetSelectionClose()
    {
        LPresetDraftChanged -= LPresetDraftHandle;
        LPresetNameChanged -= LPresetNameHandle;
    }

    public void LPresetSelectionSelect(string lPresetName)
    {
        if (!LPresetNameCheck(lPresetName))
        {
            return;
        }

        LPresetSelectionName = lPresetName;
        LPresetDraftAttach(lPresetName);
        LPresetSelectionRaise();
    }

    public bool LPresetSelectionCommit(string lOldName, string lNewName)
    {
        string lName = lNewName.Trim();
        if (string.IsNullOrWhiteSpace(lName)
            || string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase)
            || (LPresetNativeSeam?.Invoke(lOldName) ?? LPreset.LPresetNativeCheck(lOldName))
            || LPresetNameCheck(lName))
        {
            return false;
        }

        if (LPresetLoadSeam?.Invoke(lOldName) is not { } lPresetStored)
        {
            return false;
        }

        lPresetStored.LPresetName = lName;
        LPresetDraftMove(lOldName, lName);
        if (LPresetRenameSeam?.Invoke(lOldName, lName, lPresetStored) == true)
        {
            return true;
        }

        LPresetDraftMove(lName, lOldName);
        return false;
    }

    public bool LPresetSelectionSave(string lPresetName)
    {
        string lName = lPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lName))
        {
            return false;
        }

        string lOldName = LPresetSelectionName;
        bool lPresetRenamed = !string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase);
        LPresetDocument lRecord;
        LPresetDocument? lPresetPrior;
        lock (LPresetDraftLock)
        {
            lRecord = LPresetDraftRead(lOldName);
            LPresetDrafts.TryGetValue(lName, out lPresetPrior);
            lRecord.LPresetName = lName;
            LPresetSelectionName = lName;
            LPresetDrafts[lName] = lRecord;
        }

        if (LPresetSaveSeam?.Invoke(lName, lRecord) != true)
        {
            lRecord.LPresetName = lOldName;
            LPresetSelectionName = lOldName;
            lock (LPresetDraftLock)
            {
                LPresetDrafts[lOldName] = lRecord;
                if (lPresetRenamed)
                {
                    LPresetDraftRestore(lName, lPresetPrior);
                }
            }

            return false;
        }

        if (lPresetRenamed)
        {
            lock (LPresetDraftLock)
            {
                LPresetDrafts.Remove(lOldName);
            }

            LPresetDraftReset(lOldName);
        }

        LPresetDraftRaise(lName);
        return true;
    }

    public void LPresetSelectionRestore() => LPresetDraftReset(LPresetSelectionName);

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

            LPresetNameChanged?.Invoke(lPresetName, LPreset.LPresetFirstName ?? string.Empty);
            return;
        }

        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetDocument? lPresetDraft)
                || lPresetPrevious is null
                || !LPreset.LPresetRecordMatch(lPresetPrevious, lPresetDraft))
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

    private static bool LPresetNameCheck(string lPresetName) =>
        LPreset.LPresetNames.Any(lName =>
            string.Equals(lName, lPresetName, StringComparison.OrdinalIgnoreCase));

    private static string LPresetSelectionResolve(string lPresetName) =>
        LPresetNameCheck(lPresetName) ? lPresetName : LPreset.LPresetFirstName ?? string.Empty;

    private static void LPresetDraftAccept(string lPresetName, LPresetDocument lPresetValue)
    {
        lPresetValue.LPresetName = lPresetName;
        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetDocument? lPresetDraft)
                || (LPresetLoadSeam?.Invoke(lPresetName) is { } lPresetStored
                    && LPreset.LPresetRecordMatch(lPresetStored, lPresetDraft)))
            {
                LPresetDrafts[lPresetName] = lPresetValue;
            }
        }
    }

    private static LPresetDocument LPresetDraftRead(string lPresetName)
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

    private static void LPresetDraftSet(string lPresetName, LPresetDocument lPresetValue)
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

    private static void LPresetDraftReset(string lPresetName)
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

    private void LPresetDraftAttach(string lPresetName)
    {
        lock (LPresetDraftLock)
        {
            if (!LPresetDrafts.ContainsKey(lPresetName))
            {
                LPresetDrafts[lPresetName] = LPresetDraftCreate(lPresetName);
            }
        }
    }

    private void LPresetDraftHandle(string lPresetName)
    {
        if (string.Equals(lPresetName, LPresetSelectionName, StringComparison.OrdinalIgnoreCase))
        {
            LPresetSelectionRaise();
        }
    }

    private void LPresetNameHandle(string lOldPresetName, string lNewPresetName)
    {
        if (!string.Equals(lOldPresetName, LPresetSelectionName, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        LPresetSelectionName = lNewPresetName;
        LPresetDraftAttach(lNewPresetName);
        LPresetSelectionRaise();
    }

    private void LPresetSelectionRaise() => LPresetSelectionChanged?.Invoke();
}
