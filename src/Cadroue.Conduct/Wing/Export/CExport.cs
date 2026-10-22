using System;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed class CExport(
    Func<string> selected,
    Func<LPresetDocument> working,
    Func<string, bool> save,
    Func<string, string> nameCreate,
    Func<string, bool> native,
    Func<string, bool> delete,
    Action restore)
{
    public string? CExportSelected
    {
        get
        {
            string name = selected();
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }

    public LPresetDocument CExportWorkingRead() => working();

    public (bool?, string) CExportPresetAdd(string presetName)
    {
        string name = nameCreate(presetName);
        return (save(name), name);
    }

    public bool? CExportPresetDelete()
    {
        if (CExportSelected is not string presetName || native(presetName))
        {
            return null;
        }

        return delete(presetName);
    }

    public bool? CExportModificationApply() => CExportSelectedSave();

    private bool? CExportSelectedSave()
    {
        if (CExportSelected is not string presetName || native(presetName))
        {
            return null;
        }

        return save(presetName);
    }

    public bool? CExportModificationRestore() => CExportSelectedRestore();

    private bool? CExportSelectedRestore()
    {
        if (CExportSelected is null)
        {
            return null;
        }

        restore();
        return true;
    }
}
