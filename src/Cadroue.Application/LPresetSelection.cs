using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LPresetSelection
{
    private static readonly Dictionary<string, LPresetRecord> LPresetDrafts =
        new(StringComparer.OrdinalIgnoreCase);

    private static event Action<string>? LPresetDraftChange;

    public static Func<string, LPresetRecord?>? LPresetLoadSeam;
    public static Func<string, LPresetRecord, bool>? LPresetSaveSeam;
    public static Func<string, string, LPresetRecord, bool>? LPresetRenameSeam;
    public static Func<LPresetRecord, LEncoding>? LPresetOutputSeam;
    public static Func<string, bool>? LPresetNativeSeam;

    public LPresetSelection(string lPresetName)
    {
        LPresetSelectionName = lPresetName;
        LPresetDraftAttach(lPresetName);
        LPresetDraftChange += LPresetDraftHandle;
    }

    public LPresetSelection(LPresetRecord lPresetValue, string lPresetName)
    {
        LPresetSelectionName = lPresetName;
        lPresetValue.LPresetName = lPresetName;
        LPresetDrafts[lPresetName] = lPresetValue;
        LPresetDraftChange += LPresetDraftHandle;
    }

    public event Action? LPresetSelectionChange;

    public string LPresetSelectionName { get; private set; }

    public LPresetRecord LPresetSelectionValue
    {
        get => LPresetDraftRead(LPresetSelectionName);
        set
        {
            LPresetSelectionName = value.LPresetName;
            LPresetDraftSet(value.LPresetName, value);
        }
    }

    public bool LPresetSelectionValid =>
        LPreset.LPresetNames.Any(lName =>
            string.Equals(lName, LPresetSelectionName, StringComparison.OrdinalIgnoreCase));

    public LEncoding? LPresetSelectionEncoding =>
        LPresetSelectionValid ? LPresetOutputSeam?.Invoke(LPresetSelectionValue) : null;

    public void LPresetSelectionClose() => LPresetDraftChange -= LPresetDraftHandle;

    public void LPresetSelectionSelect(string lPresetName)
    {
        LPresetSelectionName = lPresetName;
        LPresetDraftAttach(lPresetName);
        LPresetSelectionRaise();
    }

    public bool LPresetSelectionSet(string lPresetName)
    {
        string lName = lPresetName.Trim();
        string lOldName = LPresetSelectionName;
        if (string.IsNullOrWhiteSpace(lName) || string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        LPresetRecord lRecord = LPresetDraftRead(lOldName);
        lRecord.LPresetName = lName;
        LPresetSelectionName = lName;
        if (LPresetRenameSeam is null || !LPresetRenameSeam(lOldName, lName, lRecord))
        {
            lRecord.LPresetName = lOldName;
            LPresetSelectionName = lOldName;
            return false;
        }

        LPresetDraftMove(lOldName, lName);
        return true;
    }

    public bool LPresetSelectionCommit(string lOldName, string lNewName)
    {
        string lName = lNewName.Trim();
        if (string.IsNullOrWhiteSpace(lName)
            || string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase)
            || (LPresetNativeSeam?.Invoke(lOldName) ?? LPreset.LPresetNativeCheck(lOldName))
            || LPreset.LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (string.Equals(lOldName, LPresetSelectionName, StringComparison.OrdinalIgnoreCase))
        {
            return LPresetSelectionSet(lName);
        }

        LPresetRecord lRecord = LPresetDraftRead(lOldName);
        lRecord.LPresetName = lName;
        if (LPresetRenameSeam?.Invoke(lOldName, lName, lRecord) != true)
        {
            lRecord.LPresetName = lOldName;
            return false;
        }

        LPresetDraftMove(lOldName, lName);
        return true;
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
        LPresetRecord lRecord = LPresetDraftRead(lOldName);
        LPresetDrafts.TryGetValue(lName, out LPresetRecord? lPresetPrior);
        lRecord.LPresetName = lName;
        LPresetSelectionName = lName;
        LPresetDrafts[lName] = lRecord;
        if (LPresetSaveSeam?.Invoke(lName, lRecord) != true)
        {
            lRecord.LPresetName = lOldName;
            LPresetSelectionName = lOldName;
            LPresetDrafts[lOldName] = lRecord;
            if (lPresetRenamed)
            {
                LPresetDraftRestore(lName, lPresetPrior);
            }

            return false;
        }

        if (lPresetRenamed)
        {
            LPresetDrafts.Remove(lOldName);
            LPresetDraftReset(lOldName);
        }

        LPresetDraftRaise(lName);
        return true;
    }

    public void LPresetSelectionRestore() => LPresetDraftReset(LPresetSelectionName);

    internal static void LPresetDraftSync(string lPresetName, LPresetRecord? lPresetStored, LPresetRecord? lPresetPrevious)
    {
        if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetRecord? lPresetDraft))
        {
            return;
        }

        if (lPresetStored is null)
        {
            LPresetDrafts.Remove(lPresetName);
            LPresetDraftRaise(lPresetName);
            return;
        }

        if (lPresetPrevious is null || !LPreset.LPresetRecordMatch(lPresetPrevious, lPresetDraft))
        {
            return;
        }

        LPresetDrafts[lPresetName] = LPresetDraftCopy(lPresetStored, lPresetName);
        LPresetDraftRaise(lPresetName);
    }

    internal static void LPresetDraftMove(string lOldPresetName, string lNewPresetName)
    {
        if (!string.Equals(lOldPresetName, lNewPresetName, StringComparison.OrdinalIgnoreCase)
            && LPresetDrafts.Remove(lOldPresetName, out LPresetRecord? lPresetDraft))
        {
            lPresetDraft.LPresetName = lNewPresetName;
            LPresetDrafts[lNewPresetName] = lPresetDraft;
            LPresetDraftRaise(lOldPresetName);
        }

        LPresetDraftRaise(lNewPresetName);
    }

    private static LPresetRecord LPresetDraftRead(string lPresetName)
    {
        if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetRecord? lPresetDraft))
        {
            lPresetDraft = LPresetDraftCreate(lPresetName);
            LPresetDrafts[lPresetName] = lPresetDraft;
        }

        return lPresetDraft;
    }

    private static LPresetRecord LPresetDraftCreate(string lPresetName)
    {
        LPresetRecord lPresetDraft = LPresetLoadSeam?.Invoke(lPresetName) ?? new LPresetRecord();
        lPresetDraft.LPresetName = lPresetName;
        return lPresetDraft;
    }

    private static void LPresetDraftSet(string lPresetName, LPresetRecord lPresetValue)
    {
        lPresetValue.LPresetName = lPresetName;
        LPresetDrafts[lPresetName] = lPresetValue;
        LPresetDraftRaise(lPresetName);
    }

    private static void LPresetDraftRestore(string lPresetName, LPresetRecord? lPresetPrior)
    {
        if (lPresetPrior is null)
        {
            LPresetDrafts.Remove(lPresetName);
            return;
        }

        LPresetDrafts[lPresetName] = lPresetPrior;
    }

    private static void LPresetDraftReset(string lPresetName)
    {
        if (LPresetLoadSeam?.Invoke(lPresetName) is not { } lPresetStored)
        {
            return;
        }

        LPresetDrafts[lPresetName] = LPresetDraftCopy(lPresetStored, lPresetName);
        LPresetDraftRaise(lPresetName);
    }

    private static LPresetRecord LPresetDraftCopy(LPresetRecord lPresetRecord, string lPresetName)
    {
        LPresetRecord lPresetDraft = LPreset.LPresetStateCreate(lPresetRecord).LPresetRecordCreate();
        lPresetDraft.LPresetName = lPresetName;
        return lPresetDraft;
    }

    private static void LPresetDraftRaise(string lPresetName) => LPresetDraftChange?.Invoke(lPresetName);

    private void LPresetDraftAttach(string lPresetName)
    {
        if (!LPresetDrafts.ContainsKey(lPresetName))
        {
            LPresetDrafts[lPresetName] = LPresetDraftCreate(lPresetName);
        }
    }

    private void LPresetDraftHandle(string lPresetName)
    {
        if (string.Equals(lPresetName, LPresetSelectionName, StringComparison.OrdinalIgnoreCase))
        {
            LPresetSelectionRaise();
        }
    }

    private void LPresetSelectionRaise() => LPresetSelectionChange?.Invoke();
}
