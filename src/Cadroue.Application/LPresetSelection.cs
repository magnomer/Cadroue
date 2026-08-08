using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LPresetSelection
{
    public static Func<string, LPresetRecord?>? LPresetLoadSeam;
    public static Action<string, LPresetRecord>? LPresetSaveSeam;
    public static Func<string, string, LPresetRecord, bool>? LPresetRenameSeam;
    public static Func<LPresetRecord, LEncoding>? LPresetOutputSeam;

    private LPresetRecord lPresetSelectionValue;

    public LPresetSelection(string lPresetName)
    {
        LPresetSelectionName = lPresetName;
        lPresetSelectionValue = LPresetLoadSeam?.Invoke(lPresetName) ?? new LPresetRecord { LPresetName = lPresetName };
    }

    public LPresetSelection(LPresetRecord lPresetValue, string lPresetName)
    {
        LPresetSelectionName = lPresetName;
        lPresetValue.LPresetName = lPresetName;
        lPresetSelectionValue = lPresetValue;
    }

    public event Action? LPresetSelectionChange;

    public string LPresetSelectionName { get; private set; }

    public LPresetRecord LPresetSelectionValue
    {
        get => lPresetSelectionValue;
        set
        {
            lPresetSelectionValue = value;
            LPresetSelectionName = value.LPresetName;
            LPresetSelectionRaise();
        }
    }

    public LEncoding? LPresetSelectionEncoding => LPresetOutputSeam?.Invoke(lPresetSelectionValue);

    public void LPresetSelectionSelect(string lPresetName)
    {
        LPresetSelectionName = lPresetName;
        LPresetRecord lRecord = LPresetLoadSeam?.Invoke(lPresetName) ?? lPresetSelectionValue;
        lRecord.LPresetName = lPresetName;
        LPresetSelectionValue = lRecord;
    }

    public bool LPresetSelectionSet(string lPresetName)
    {
        string lName = lPresetName.Trim();
        string lOldName = LPresetSelectionName;
        if (string.IsNullOrWhiteSpace(lName) || string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (LPresetRenameSeam is null || !LPresetRenameSeam(lOldName, lName, lPresetSelectionValue))
        {
            return false;
        }

        lPresetSelectionValue.LPresetName = lName;
        LPresetSelectionName = lName;
        LPresetSelectionRaise();
        return true;
    }

    public bool LPresetSelectionRename(string lOldName, string lNewName)
    {
        string lName = lNewName.Trim();
        if (string.IsNullOrWhiteSpace(lName)
            || string.Equals(lOldName, lName, StringComparison.OrdinalIgnoreCase)
            || LPreset.LPresetNativeCheck(lOldName)
            || LPreset.LPresetNames.Any(lExisting => string.Equals(lExisting, lName, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (string.Equals(lOldName, LPresetSelectionName, StringComparison.OrdinalIgnoreCase))
        {
            return LPresetSelectionSet(lName);
        }

        if (LPresetLoadSeam?.Invoke(lOldName) is not { } lRecord)
        {
            return false;
        }

        lRecord.LPresetName = lName;
        return LPresetRenameSeam?.Invoke(lOldName, lName, lRecord) ?? false;
    }

    public void LPresetSelectionSave(string lPresetName)
    {
        string lName = lPresetName.Trim();
        if (string.IsNullOrWhiteSpace(lName))
        {
            return;
        }

        lPresetSelectionValue.LPresetName = lName;
        LPresetSelectionName = lName;
        LPresetSaveSeam?.Invoke(lName, lPresetSelectionValue);
        LPresetSelectionRaise();
    }

    public void LPresetSelectionRestore()
    {
        if (LPresetLoadSeam?.Invoke(LPresetSelectionName) is { } lRecord)
        {
            lRecord.LPresetName = LPresetSelectionName;
            LPresetSelectionValue = lRecord;
        }
    }

    private void LPresetSelectionRaise() => LPresetSelectionChange?.Invoke();
}
