using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class LExport
{
    private readonly LPresetSelection lExportOwner;
    private readonly bool lExportSmartAllowed;
    private string? lExportSelected;
    private string? lExportEditing;
    private string? lExportDragging;
    private bool lExportDragActive;

    public event Action? LExportPresetsChange;

    public LExport(LPresetSelection lExportSelection, bool lSmartAllowed)
    {
        lExportOwner = lExportSelection;
        lExportSmartAllowed = lSmartAllowed;
        lExportSelected = LExportNameNormalize(lExportSelection.LPresetSelectionName);
    }

    public string? LExportSelected => lExportSelected;

    public string? LExportEditing => lExportEditing;

    public string? LExportDragging => lExportDragging;

    public bool LExportDragActive => lExportDragActive;

    public bool LExportSmartAllowed => lExportSmartAllowed;

    public bool LExportSelectedCheck(string lPresetName) =>
        string.Equals(lPresetName, lExportSelected, StringComparison.OrdinalIgnoreCase);

    public bool LExportEditingCheck(string lPresetName) =>
        string.Equals(lPresetName, lExportEditing, StringComparison.OrdinalIgnoreCase);

    public void LExportAttach()
    {
        LPreset.LPresetStoreChange += LExportSync;
        lExportOwner.LPresetSelectionChange += LExportUpdate;
        LExportSync();
    }

    public void LExportDetach()
    {
        LPreset.LPresetStoreChange -= LExportSync;
        lExportOwner.LPresetSelectionChange -= LExportUpdate;
    }

    public void LExportSync()
    {
        if (lExportEditing is null && !lExportDragActive)
        {
            LExportUpdate();
        }
    }

    public void LExportUpdate()
    {
        lExportSelected = LExportNameNormalize(lExportOwner.LPresetSelectionName);
        if (!string.Equals(lExportEditing, lExportSelected, StringComparison.OrdinalIgnoreCase))
        {
            lExportEditing = null;
        }

        LExportPresetsChange?.Invoke();
    }

    public void LExportSelect(string lPresetName)
    {
        lExportSelected = lPresetName;
        lExportOwner.LPresetSelectionSelect(lPresetName);
    }

    public void LExportEditStart(string lPresetName)
    {
        if (!LExportSelectedCheck(lPresetName))
        {
            LExportSelect(lPresetName);
        }

        lExportEditing = lPresetName;
        LExportPresetsChange?.Invoke();
    }

    public void LExportEditCancel()
    {
        lExportEditing = null;
        LExportSync();
    }

    public bool LExportNameCommit(string lOldName, string lNewName)
    {
        if (!LExportEditingCheck(lOldName))
        {
            return false;
        }

        lExportEditing = null;
        lExportOwner.LPresetSelectionCommit(lOldName, lNewName);
        LExportSync();
        return true;
    }

    public void LExportDragStart(string lPresetName)
    {
        lExportDragging = lPresetName;
        lExportDragActive = false;
    }

    public bool LExportDragMove()
    {
        if (lExportDragging is null || lExportEditing is not null)
        {
            return false;
        }

        lExportDragActive = true;
        return true;
    }

    public bool LExportDragClear()
    {
        bool lMoved = lExportDragActive;
        lExportDragging = null;
        lExportDragActive = false;
        if (lMoved)
        {
            LExportSync();
        }

        return lMoved;
    }

    private static string? LExportNameNormalize(string lPresetName) =>
        string.IsNullOrEmpty(lPresetName) ? null : lPresetName;
}
