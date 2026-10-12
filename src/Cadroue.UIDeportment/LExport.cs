using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class LExport
{
    private readonly CUsher lExportUsher;
    private readonly LPresetSelection lExportOwner;
    private readonly bool lExportSmartAllowed;
    private readonly string lExportNotice;
    private string? lExportSelected;
    private string? lExportEditing;
    private string? lExportDragging;
    private bool lExportDragActive;
    private LGhost lExportGhost = new(0, 0);

    public event Action? LExportPresetsChanged;

    public LExport(CUsher lUsher, LPresetSelection lExportSelection, QWorkspaceKey qWorkspaceKey)
    {
        lExportUsher = lUsher;
        lExportOwner = lExportSelection;
        lExportSmartAllowed = qWorkspaceKey.LWorkspaceSmart;
        lExportNotice = qWorkspaceKey.LWorkspaceNotice;
        lExportSelected = LExportNameNormalize(lExportSelection.LPresetSelectionName);
    }

    public string? LExportSelected => lExportSelected;

    public string? LExportEditing => lExportEditing;

    public string? LExportDragging => lExportDragging;

    public bool LExportDragActive => lExportDragActive;

    public LGhost LExportGhost => lExportGhost;

    public bool LExportSelectedCheck(string lPresetName) =>
        string.Equals(lPresetName, lExportSelected, StringComparison.OrdinalIgnoreCase);

    public bool LExportEditingCheck(string lPresetName) =>
        string.Equals(lPresetName, lExportEditing, StringComparison.OrdinalIgnoreCase);

    public string LExportSupportNotice => lExportNotice;

    public bool LExportSupportCheck(string lPresetName, LPreset lWorking) =>
        lExportOwner.LPresetSupportCheck(lPresetName, lWorking, LExportSelectedCheck(lPresetName));

    public LSEncoder LExportEncoderCreate(LPreset lSource) => new(lExportUsher, lSource, lExportSmartAllowed);

    public void LExportAttach()
    {
        LPreset.LPresetStoreChanged += LExportSync;
        lExportOwner.LPresetSelectionChanged += LExportUpdate;
        LExportSync();
    }

    public void LExportDetach()
    {
        LPreset.LPresetStoreChanged -= LExportSync;
        lExportOwner.LPresetSelectionChanged -= LExportUpdate;
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

        LExportPresetsChanged?.Invoke();
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
        LExportPresetsChanged?.Invoke();
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

    public void LExportDragStart(string lPresetName, double lGrabX, double lGrabY)
    {
        lExportDragging = lPresetName;
        lExportDragActive = false;
        lExportGhost = new LGhost(lGrabX, lGrabY);
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
