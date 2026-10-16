using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LPresetSelection
{
    public static Func<string, LPresetDocument, bool>? LPresetSaveSeam;
    public static Func<string, string, LPresetDocument, bool>? LPresetRenameSeam;
    public static Func<LPresetDocument, LEncoding>? LPresetOutputSeam;
    public static Func<string, bool>? LPresetNativeSeam;

    private readonly LWorkKind? lPresetSelectionKind;

    public LPresetSelection(string lPresetName, string lKey)
    {
        lPresetSelectionKind = lKey switch
        {
            "Edit" => LWorkKind.LWorkKindEdit,
            "Audio" => LWorkKind.LWorkKindAudio,
            _ => null
        };
        LPresetSelectionName = LPresetSelectionResolve(lPresetName);
        LPresetDraft.LPresetDraftAttach(LPresetSelectionName);
        LPresetDraft.LPresetDraftChanged += LPresetDraftHandle;
        LPresetDraft.LPresetNameChanged += LPresetNameHandle;
    }

    public event Action? LPresetSelectionChanged;

    public string LPresetSelectionName { get; private set; }

    public LPresetDocument LPresetSelectionValue
    {
        get => LPresetDraft.LPresetDraftRead(LPresetSelectionName);
        set
        {
            if (LPresetNameCheck(value.LPresetName))
            {
                LPresetSelectionName = value.LPresetName;
            }

            LPresetDraft.LPresetDraftSet(LPresetSelectionName, value);
        }
    }

    public bool LPresetSelectionValid => LPresetNameCheck(LPresetSelectionName);

    public LEncoding? LPresetSelectionEncoding => LPresetEncodingRead(LPresetSelectionName);

    // The selected encoding supports this selection's own kind; a selection without a kind, or without an
    // encoding, accepts any.
    public bool LPresetSelectionSupported =>
        LPresetSelectionEncoding is not { } lEncoding
        || lPresetSelectionKind is not { } lKind
        || lEncoding.LEncodingSupportCheck(lKind);

    // The encoding a preset name yields: none for a name the catalogue does not hold.
    public static LEncoding? LPresetEncodingRead(string lPresetName) =>
        LPresetNameCheck(lPresetName) ? LPresetOutputSeam?.Invoke(LPresetDraft.LPresetDraftRead(lPresetName)) : null;

    public void LPresetSelectionAccept(LPresetDocument lPresetValue, string lPresetName)
    {
        LPresetSelectionName = LPresetSelectionResolve(lPresetName);
        LPresetDraft.LPresetDraftAttach(LPresetSelectionName);
        if (string.Equals(LPresetSelectionName, lPresetName, StringComparison.OrdinalIgnoreCase))
        {
            LPresetDraft.LPresetDraftAccept(lPresetName, lPresetValue);
        }
    }

    public void LPresetSelectionClose()
    {
        LPresetDraft.LPresetDraftChanged -= LPresetDraftHandle;
        LPresetDraft.LPresetNameChanged -= LPresetNameHandle;
    }

    public void LPresetSelectionSelect(string lPresetName)
    {
        if (!LPresetNameCheck(lPresetName))
        {
            return;
        }

        LPresetSelectionName = lPresetName;
        LPresetDraft.LPresetDraftAttach(lPresetName);
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

        if (LPresetDraft.LPresetLoadSeam?.Invoke(lOldName) is not { } lPresetStored)
        {
            return false;
        }

        lPresetStored.LPresetName = lName;
        LPresetDraft.LPresetDraftMove(lOldName, lName);
        if (LPresetRenameSeam?.Invoke(lOldName, lName, lPresetStored) == true)
        {
            return true;
        }

        LPresetDraft.LPresetDraftMove(lName, lOldName);
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
        LPresetDocument lRecord = LPresetDraft.LPresetDraftPrepare(lOldName, lName, out LPresetDocument? lPresetPrior);
        LPresetSelectionName = lName;
        if (LPresetSaveSeam?.Invoke(lName, lRecord) != true)
        {
            LPresetSelectionName = lOldName;
            LPresetDraft.LPresetDraftCancel(lOldName, lRecord, lName, lPresetPrior);
            return false;
        }

        LPresetDraft.LPresetDraftCommit(lOldName, lName);
        return true;
    }

    public void LPresetSelectionRestore() => LPresetDraft.LPresetDraftReset(LPresetSelectionName);

    public bool LPresetSupportCheck(string lPresetName, LPreset lWorking, bool lSelected)
    {
        if (lPresetSelectionKind is not { } lKind)
        {
            return true;
        }

        LPreset? lPresetValue = lSelected ? lWorking : LPreset.LPresetRead(lPresetName);
        return lPresetValue is null
            || LEncoding.LEncodingSupportCheck(
                lKind,
                lPresetValue.LPresetVideo.LPresetMode,
                lPresetValue.LPresetAudio.LPresetMode,
                lPresetValue.LPresetAudio.LPresetStream);
    }

    private static bool LPresetNameCheck(string lPresetName) =>
        LPreset.LPresetNames.Any(lName =>
            string.Equals(lName, lPresetName, StringComparison.OrdinalIgnoreCase));

    private static string LPresetSelectionResolve(string lPresetName) =>
        LPresetNameCheck(lPresetName) ? lPresetName : LPreset.LPresetFirstName ?? string.Empty;

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
        LPresetDraft.LPresetDraftAttach(lNewPresetName);
        LPresetSelectionRaise();
    }

    private void LPresetSelectionRaise() => LPresetSelectionChanged?.Invoke();
}
