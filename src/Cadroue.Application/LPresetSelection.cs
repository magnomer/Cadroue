using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LPresetSelection
{
    private static readonly Dictionary<string, LPresetRecord> LPresetDrafts =
        new(StringComparer.OrdinalIgnoreCase);

    private static event Action<string>? LPresetDraftChange;

    private static event Action<string, string>? LPresetNameChange;

    public static Func<string, LPresetRecord?>? LPresetLoadSeam;
    public static Func<string, LPresetRecord, bool>? LPresetSaveSeam;
    public static Func<string, string, LPresetRecord, bool>? LPresetRenameSeam;
    public static Func<LPresetRecord, LEncoding>? LPresetOutputSeam;
    public static Func<string, bool>? LPresetNativeSeam;

    public LPresetSelection(string lPresetName)
    {
        LPresetSelectionName = LPresetSelectionResolve(lPresetName);
        LPresetDraftAttach(LPresetSelectionName);
        LPresetDraftChange += LPresetDraftHandle;
        LPresetNameChange += LPresetNameHandle;
    }

    public LPresetSelection(LPresetRecord lPresetValue, string lPresetName)
        : this(lPresetName)
    {
        if (string.Equals(LPresetSelectionName, lPresetName, StringComparison.OrdinalIgnoreCase))
        {
            LPresetDraftAccept(lPresetName, lPresetValue);
        }
    }

    public event Action? LPresetSelectionChange;

    public string LPresetSelectionName { get; private set; }

    public LPresetRecord LPresetSelectionValue
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
        LPresetDraftChange -= LPresetDraftHandle;
        LPresetNameChange -= LPresetNameHandle;
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

    internal static void LPresetDraftSync(
        string lPresetName,
        LPresetRecord? lPresetStored,
        LPresetRecord? lPresetPrevious)
    {
        if (lPresetStored is null)
        {
            LPresetDrafts.Remove(lPresetName);
            LPresetNameChange?.Invoke(lPresetName, LPreset.LPresetFirstName ?? string.Empty);
            return;
        }

        if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetRecord? lPresetDraft)
            || lPresetPrevious is null
            || !LPreset.LPresetRecordMatch(lPresetPrevious, lPresetDraft))
        {
            return;
        }

        LPresetDrafts[lPresetName] = LPresetDraftCopy(lPresetStored, lPresetName);
        LPresetDraftRaise(lPresetName);
    }

    internal static void LPresetDraftMove(string lOldPresetName, string lNewPresetName)
    {
        if (LPresetDrafts.Remove(lOldPresetName, out LPresetRecord? lPresetDraft))
        {
            lPresetDraft.LPresetName = lNewPresetName;
            LPresetDrafts[lNewPresetName] = lPresetDraft;
        }

        LPresetNameChange?.Invoke(lOldPresetName, lNewPresetName);
    }

    private static bool LPresetNameCheck(string lPresetName) =>
        LPreset.LPresetNames.Any(lName =>
            string.Equals(lName, lPresetName, StringComparison.OrdinalIgnoreCase));

    private static string LPresetSelectionResolve(string lPresetName) =>
        LPresetNameCheck(lPresetName) ? lPresetName : LPreset.LPresetFirstName ?? string.Empty;

    private static void LPresetDraftAccept(string lPresetName, LPresetRecord lPresetValue)
    {
        lPresetValue.LPresetName = lPresetName;
        if (!LPresetDrafts.TryGetValue(lPresetName, out LPresetRecord? lPresetDraft)
            || (LPresetLoadSeam?.Invoke(lPresetName) is { } lPresetStored
                && LPreset.LPresetRecordMatch(lPresetStored, lPresetDraft)))
        {
            LPresetDrafts[lPresetName] = lPresetValue;
        }
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

    private void LPresetSelectionRaise() => LPresetSelectionChange?.Invoke();
}
