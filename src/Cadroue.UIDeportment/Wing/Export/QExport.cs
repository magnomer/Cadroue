using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QExport
{
    private readonly CUsher qExportUsher;
    private readonly CSEncoder qExportEncoder;
    private readonly CExportRow cExportRow;
    private readonly LPresetSelection qExportOwner;
    private readonly bool qExportSmartAllowed;
    private readonly string qExportNotice;
    private bool _qExportRequestActive;

    public event Action? QExportPresetsChanged;

    public QExport(
        CUsher lUsher, CSEncoder lEncoder, LPresetSelection lExportSelection, QWorkspaceFacts qWorkspaceKey)
    {
        qExportUsher = lUsher;
        qExportEncoder = lEncoder;
        qExportOwner = lExportSelection;
        qExportSmartAllowed = qWorkspaceKey.QWorkspaceSmart;
        qExportNotice = qWorkspaceKey.QWorkspaceNotice;
        CExport = new CExport(
            () => lExportSelection.LPresetSelectionName,
            () => lExportSelection.LPresetSelectionValue,
            lExportSelection.LPresetSelectionSave,
            LPresetNaming.LPresetNameCreate,
            LPresetTable.LPresetNativeCheck,
            LPresetTable.LPresetDelete,
            lExportSelection.LPresetSelectionRestore);
        cExportRow = new CExportRow(
            LPresetTable.LPresetNativeCheck,
            (name, working) => LPresetTable.LPresetMatch(name, LPreset.LPresetStateCreate(working)),
            (name, working, selected) => lExportSelection.LPresetSupportCheck(
                name,
                LPreset.LPresetStateCreate(working),
                selected));
        QExportTransfer = new QExportTransfer(new CExportTransfer(
            () => lExportSelection.LPresetSelectionValue,
            () => CExport.CExportSelected,
            LPresetStore.LPresetCatalogCheck,
            LPresetStore.LPresetFileSave,
            LPresetStore.LPresetFileLoad,
            LPresetNaming.LPresetNameResolve,
            LPresetNaming.LPresetNameCreate,
            (name, record) => LPresetTable.LPresetSave(name, LPreset.LPresetStateCreate(record)),
            lExportSelection.LPresetSelectionSelect,
            LPresetNaming.LPresetFileFormat));
        QExportDrag = new QExportDrag(this, LPresetTable.LPresetMove);
        QExportList = new QExportList();
        QExportName = new(
            () => QExportSelected,
            QExportSelect,
            (oldName, newName) => qExportOwner.LPresetSelectionCommit(oldName, newName),
            () => QExportPresetsChanged?.Invoke(),
            QExportSync);
        QExportSummary = new(QExportSummaryRead);
    }

    public CExport CExport { get; }

    public QExportDrag QExportDrag { get; }

    public QExportTransfer QExportTransfer { get; }

    public QExportList QExportList { get; }

    public QExportName QExportName { get; }

    public QExportSummary QExportSummary { get; }

    public string? QExportSelected => CExport.CExportSelected;

    public string? QExportEditing => QExportName.QExportNameEditing;

    public bool QExportEditingCheck(string lPresetName) => QExportName.QExportNameCheck(lPresetName);

    public LPreset QExportWorkingRead() => LPreset.LPresetStateCreate(CExport.CExportWorkingRead());

    public QExportRows QExportRowsRead()
    {
        LPresetDocument working = CExport.CExportWorkingRead();
        return new(
            cExportRow,
            working,
            () => QExportSelected,
            QExportEditingCheck,
            LPresetNaming.LPresetDisplayRead,
            qExportNotice);
    }

    public QExportDigest QExportSummaryRead()
    {
        LPreset lWorking = QExportWorkingRead();
        return new(
            lWorking.LPresetVideoSummary,
            lWorking.LPresetAudioSummary,
            lWorking.LPresetContainer,
            lWorking.LPresetOutputSummary);
    }

    public QSEncoder QExportEncoderCreate() =>
        new(qExportUsher, qExportEncoder, qExportOwner.LPresetSelectionName, qExportSmartAllowed);

    public void QExportFailureRaise()
    {
        QExportFailureRaise(CExport.CExportModificationRestore, string.Empty, _ => { });
    }

    public void QExportFailureRaise(Func<string> presetName, Action<string> failure)
    {
        string name = presetName();
        QExportFailureRaise(
            () =>
            {
                (bool? result, string resolved) = CExport.CExportPresetAdd(name);
                name = resolved;
                return result;
            },
            string.Empty,
            _ => failure(name));
    }

    public void QExportFailureRaise(Action<string> failure)
    {
        string name = QExportSelected ?? string.Empty;
        QExportFailureRaise(CExport.CExportPresetDelete, name, failure);
    }

    public void QExportFailureRaise(string failureName, Action<string> failure)
    {
        QExportFailureRaise(CExport.CExportModificationApply, failureName, failure);
    }

    private void QExportFailureRaise(Func<bool?> request, string failureName, Action<string> failure)
    {
        bool? result;
        _qExportRequestActive = true;
        try
        {
            result = request();
        }
        finally
        {
            _qExportRequestActive = false;
        }

        if (result == true)
        {
            QExportUpdate();
        }
        else if (result == false)
        {
            failure(failureName);
        }
    }

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
        QExportName.QExportNameDetach();
        LPresetTable.LPresetStoreChanged -= QExportSync;
        qExportOwner.LPresetSelectionChanged -= QExportUpdate;
    }

    public void QExportSync()
    {
        if (QExportName.QExportNameEditing is null && !QExportDrag.QExportDragActive)
        {
            QExportUpdate();
        }
    }

    public void QExportUpdate()
    {
        if (_qExportRequestActive)
        {
            return;
        }

        QExportName.QExportNameSync(QExportSelected);
        QExportPresetsChanged?.Invoke();
    }

    public void QExportSelect(string lPresetName)
    {
        qExportOwner.LPresetSelectionSelect(lPresetName);
    }

    public void QExportEditStart(string lPresetName) => QExportName.QExportNameStart(lPresetName);
}
