using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QExport
{
    private readonly CUsher qExportUsher;
    private readonly CSEncoder qExportEncoder;
    private readonly LPresetSelection qExportOwner;
    private readonly bool qExportSmartAllowed;
    private readonly string qExportNotice;
    private string? qExportSelected;
    private string? qExportEditing;

    public event Action? QExportPresetsChanged;

    public QExport(
        CUsher lUsher, CSEncoder lEncoder, LPresetSelection lExportSelection, QWorkspaceFacts qWorkspaceKey)
    {
        qExportUsher = lUsher;
        qExportEncoder = lEncoder;
        qExportOwner = lExportSelection;
        qExportSmartAllowed = qWorkspaceKey.QWorkspaceSmart;
        qExportNotice = qWorkspaceKey.QWorkspaceNotice;
        qExportSelected = QExportNameNormalize(lExportSelection.LPresetSelectionName);
        QExportDrag = new QExportDrag(this);
    }

    public QExportDrag QExportDrag { get; }

    public string? QExportSelected => qExportSelected;

    public string? QExportEditing => qExportEditing;

    public bool QExportSelectedCheck(string lPresetName) =>
        string.Equals(lPresetName, qExportSelected, StringComparison.OrdinalIgnoreCase);

    public bool QExportEditingCheck(string lPresetName) =>
        string.Equals(lPresetName, qExportEditing, StringComparison.OrdinalIgnoreCase);

    public string QExportSupportNotice => qExportNotice;

    public bool QExportSupportCheck(string lPresetName, LPreset lWorking) =>
        qExportOwner.LPresetSupportCheck(lPresetName, lWorking, QExportSelectedCheck(lPresetName));

    public QSEncoder QExportEncoderCreate(LPreset lSource) =>
        new(qExportUsher, qExportEncoder, lSource, qExportSmartAllowed);

    public void QExportAttach()
    {
        QExportHandlerAttach();
        QExportSync();
    }

    private void QExportHandlerAttach()
    {
        LPresetTable.LPresetStoreChanged += QExportSync;
        qExportOwner.LPresetSelectionChanged += QExportUpdate;
    }

    public void QExportDetach()
    {
        LPresetTable.LPresetStoreChanged -= QExportSync;
        qExportOwner.LPresetSelectionChanged -= QExportUpdate;
    }

    public void QExportSync()
    {
        if (qExportEditing is null && !QExportDrag.QExportDragActive)
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

    private static string? QExportNameNormalize(string lPresetName) =>
        string.IsNullOrEmpty(lPresetName) ? null : lPresetName;
}
