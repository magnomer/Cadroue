using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QExport
{
    private readonly CUsher qExportUsher;
    private readonly LPresetSelection qExportOwner;
    private readonly bool qExportSmartAllowed;
    private readonly string qExportNotice;
    private string? qExportSelected;
    private string? qExportEditing;
    private string? qExportDragging;
    private bool qExportDragActive;
    private QGhost qExportGhost = new(0, 0);

    public event Action? QExportPresetsChanged;

    public QExport(CUsher lUsher, LPresetSelection lExportSelection, QWorkspaceFacts qWorkspaceKey)
    {
        qExportUsher = lUsher;
        qExportOwner = lExportSelection;
        qExportSmartAllowed = qWorkspaceKey.QWorkspaceSmart;
        qExportNotice = qWorkspaceKey.QWorkspaceNotice;
        qExportSelected = QExportNameNormalize(lExportSelection.LPresetSelectionName);
    }

    public string? QExportSelected => qExportSelected;

    public string? QExportEditing => qExportEditing;

    public string? QExportDragging => qExportDragging;

    public bool QExportDragActive => qExportDragActive;

    public QGhost QExportGhost => qExportGhost;

    public bool QExportSelectedCheck(string lPresetName) =>
        string.Equals(lPresetName, qExportSelected, StringComparison.OrdinalIgnoreCase);

    public bool QExportEditingCheck(string lPresetName) =>
        string.Equals(lPresetName, qExportEditing, StringComparison.OrdinalIgnoreCase);

    public string QExportSupportNotice => qExportNotice;

    public bool QExportSupportCheck(string lPresetName, LPreset lWorking) =>
        qExportOwner.LPresetSupportCheck(lPresetName, lWorking, QExportSelectedCheck(lPresetName));

    public QSEncoder QExportEncoderCreate(LPreset lSource) => new(qExportUsher, lSource, qExportSmartAllowed);

    public void QExportAttach()
    {
        QExportHandlerAttach();
        QExportSync();
    }

    private void QExportHandlerAttach()
    {
        LPreset.LPresetStoreChanged += QExportSync;
        qExportOwner.LPresetSelectionChanged += QExportUpdate;
    }

    public void QExportDetach()
    {
        LPreset.LPresetStoreChanged -= QExportSync;
        qExportOwner.LPresetSelectionChanged -= QExportUpdate;
    }

    public void QExportSync()
    {
        if (qExportEditing is null && !qExportDragActive)
        {
            QExportUpdate();
        }
    }

    public void QExportUpdate()
    {
        qExportSelected = QExportNameNormalize(qExportOwner.LPresetSelectionName);
        qExportEditing = QExportEditingResolve();
        QExportPresetsChanged?.Invoke();
    }

    public void QExportSelect(string lPresetName)
    {
        QExportSelectedSet(lPresetName);
        qExportOwner.LPresetSelectionSelect(lPresetName);
    }

    public void QExportEditStart(string lPresetName)
    {
        if (!QExportSelectedCheck(lPresetName))
        {
            QExportSelect(lPresetName);
        }

        QExportEditingSet(lPresetName);
    }

    public void QExportEditCancel()
    {
        QExportEditingClear();
        QExportSync();
    }

    public bool QExportNameCommit(string lOldName, string lNewName)
    {
        if (!QExportEditingCheck(lOldName))
        {
            return false;
        }

        QExportEditingClear();
        qExportOwner.LPresetSelectionCommit(lOldName, lNewName);
        QExportSync();
        return true;
    }

    private void QExportSelectedSet(string lPresetName) => qExportSelected = lPresetName;

    private void QExportEditingSet(string lPresetName)
    {
        qExportEditing = lPresetName;
        QExportPresetsChanged?.Invoke();
    }

    private void QExportEditingClear() => qExportEditing = null;

    // Editing survives only while it names the selected preset.
    private string? QExportEditingResolve() =>
        string.Equals(qExportEditing, qExportSelected, StringComparison.OrdinalIgnoreCase) ? qExportEditing : null;

    public void QExportDragStart(string lPresetName, double lGrabX, double lGrabY)
    {
        qExportDragging = lPresetName;
        qExportDragActive = false;
        qExportGhost = new QGhost(lGrabX, lGrabY);
    }

    public bool QExportDragMove()
    {
        if (qExportDragging is null || qExportEditing is not null)
        {
            return false;
        }

        qExportDragActive = true;
        return true;
    }

    public bool QExportDragClear()
    {
        bool lMoved = QExportDragReset();
        if (lMoved)
        {
            QExportSync();
        }

        return lMoved;
    }

    // Ends the drag; answers whether it had moved.
    private bool QExportDragReset()
    {
        bool lMoved = qExportDragActive;
        qExportDragging = null;
        qExportDragActive = false;
        return lMoved;
    }

    private static string? QExportNameNormalize(string lPresetName) =>
        string.IsNullOrEmpty(lPresetName) ? null : lPresetName;
}
