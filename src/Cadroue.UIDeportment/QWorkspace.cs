using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QWorkspace
{
    private readonly LHistory qWorkspaceHistory = new();
    private readonly CWorkspace qWorkspaceGate;
    private readonly QWorkspacePair? qWorkspacePair;
    private Func<LSceneTabEntry>? qWorkspaceLayoutSource;

    public QWorkspace(
        string lKey,
        CUsher lUsher,
        CInfo lInfo,
        CLineage lLineage,
        CWorkspace lWorkspace,
        CInspector lInspector,
        CEdit lEdit)
    {
        qWorkspaceGate = lWorkspace;
        QWorkspaceKey = lKey;
        QWorkspaceFacts = new QWorkspaceFacts(lKey);
        QWorkspaceRoster = QWorkspaceFacts.QWorkspaceWorklist ? QRoster.QRosterCreate(lUsher, lLineage) : null;
        QWorkspaceList = QWorkspaceFacts.QWorkspaceListPresent ? new QList(lUsher) : null;
        qWorkspacePair = QWorkspaceFacts.QWorkspaceFlowPresent
            ? new QWorkspacePair(QWorkspaceFacts, lUsher, lInfo, lWorkspace)
            : null;
        QWorkspacePreset = LPreset.LPresetInitialCreate(lKey);
        QWorkspacePresetOwner = new LPresetSelection(string.Empty, lKey);
        QWorkspacePresetAttach();
        QWorkspacePresetAccept(QWorkspacePreset);
        QWorkspaceExport = new QExport(lUsher, QWorkspacePresetOwner, QWorkspaceFacts);
        QWorkspaceMode = new QWorkspaceMode(QWorkspaceFacts, lUsher, lInspector, lEdit,
            QWorkspacePresetOwner, QWorkspaceViewer, QWorkspaceList, QWorkspaceFlow);
        QWorkspaceRelay = new QWorkspaceRelay(this);
    }

    public event Action<LMediaInfo, string>? QWorkspaceFlowAttached;
    public event Action? QWorkspaceFlowCleared;
    public event Action? QWorkspaceMediaClosed;
    public event Action? QWorkspaceLosslesscutFound;

    public string QWorkspaceKey { get; }

    public QWorkspaceFacts QWorkspaceFacts { get; }

    public QWorkspaceMode QWorkspaceMode { get; }

    public QWorkspaceRelay QWorkspaceRelay { get; }

    public LPreset QWorkspacePreset { get; }

    public LPresetSelection QWorkspacePresetOwner { get; }

    public QRoster? QWorkspaceRoster { get; }

    public QExport QWorkspaceExport { get; }

    public QList? QWorkspaceList { get; }

    public LDocket? QWorkspaceDocket => QWorkspaceList?.QListDocket;

    public QViewer? QWorkspaceViewer => qWorkspacePair?.QWorkspaceViewer;

    public QFlow? QWorkspaceFlow => qWorkspacePair?.QWorkspaceFlow;

    public QCompass? QWorkspaceCompass => qWorkspacePair?.QWorkspaceCompass;

    public QSource? QWorkspaceSource => qWorkspacePair?.QWorkspaceSource;

    public QInfo? QWorkspaceInfo => qWorkspacePair?.QWorkspaceInfo;

    public bool QWorkspaceFlowPresent => qWorkspacePair is not null;

    internal LSegment? QWorkspaceSegment => QWorkspaceFlow?.QFlowSection.QFlowSegment;

    public QWorkspaceFrame? QWorkspaceFrame { get; private set; }

    public FrameworkElement? QWorkspaceView => QWorkspaceFrame?.QWorkspaceFrameRoot;

    public void QWorkspaceFrameBuild(
        FrameworkElement lSurface,
        Func<UserControl> lFrameFactory,
        Func<QSource, UIElement> lSourceFactory,
        Func<QInfo, UIElement> lInfoFactory,
        Action lExportHandler,
        Func<ImageSource> lExportIcon,
        Action lSurfaceClose,
        Action lFlowClose,
        Action lViewerClose) =>
        QWorkspaceFrame = new QWorkspaceFrame(
            this, lSurface, lFrameFactory, lSourceFactory, lInfoFactory, lExportHandler, lExportIcon,
            lSurfaceClose, lFlowClose, lViewerClose);

    public void QWorkspaceFlowApply() => qWorkspacePair?.QWorkspaceFlowApply();

    public void QWorkspaceViewerApply() => qWorkspacePair?.QWorkspaceViewerApply();

    public void QWorkspaceCommandReset() => qWorkspacePair?.QWorkspaceCommandReset();

    public void QWorkspaceAttach(Func<LSceneTabEntry> lLayoutSource)
    {
        QWorkspaceHandlersAttach(lLayoutSource);
        QWorkspaceHistoryReset();
        QWorkspaceHistoryAttach();
    }

    private void QWorkspacePresetAttach() => QWorkspacePresetOwner.LPresetSelectionChanged += QWorkspacePresetHandle;

    private void QWorkspaceHandlersAttach(Func<LSceneTabEntry> lLayoutSource)
    {
        qWorkspaceLayoutSource = lLayoutSource;
        if (QWorkspaceViewer is { } lViewer)
        {
            lViewer.QViewerMediaChanged += QWorkspaceMediaHandle;
        }

        if (QWorkspaceFlow is { } lFlow)
        {
            lFlow.QFlowSection.QFlowSectionChanged += QWorkspaceSectionHandle;
            lFlow.QFlowMediaChanged += QWorkspaceHistoryReset;
        }

        if (QWorkspaceList is { } lList)
        {
            lList.QListPathChanged += QWorkspacePathHandle;
            lList.QListClearChanged += QWorkspaceRemovedHandle;
        }

        qWorkspacePair?.QWorkspacePairAttach();
    }

    private void QWorkspaceHistoryAttach() => QWorkspacePreset.LPresetChanged += QWorkspaceHistoryAdd;

    public void QWorkspaceClose()
    {
        try
        {
            QWorkspaceHandlersDetach();
            QWorkspaceRelay.QWorkspaceRelayClose();
            QWorkspacePresetDetach();
            QWorkspacePresetOwner.LPresetSelectionClose();
        }
        finally
        {
            QWorkspaceRoster?.QRosterClose();
        }
    }

    private void QWorkspaceHandlersDetach()
    {
        if (QWorkspaceViewer is not null)
        {
            QWorkspaceViewer.QViewerMediaChanged -= QWorkspaceMediaHandle;
        }

        if (QWorkspaceFlow is not null)
        {
            QWorkspaceFlow.QFlowSection.QFlowSectionChanged -= QWorkspaceSectionHandle;
            QWorkspaceFlow.QFlowMediaChanged -= QWorkspaceHistoryReset;
        }

        qWorkspacePair?.QWorkspacePairDetach();
    }

    private void QWorkspacePresetDetach()
    {
        QWorkspacePreset.LPresetChanged -= QWorkspaceHistoryAdd;
        QWorkspacePresetOwner.LPresetSelectionChanged -= QWorkspacePresetHandle;
    }

    public LStation? QWorkspaceStation => QWorkspaceRoster?.QRosterStation;

    public bool QWorkspaceBusyCheck() => QWorkspaceStation?.LStationBusyCheck() == true;

    public LSceneTabEntry QWorkspaceLayoutRead() => qWorkspaceLayoutSource?.Invoke() ?? new LSceneTabEntry();

    public LHistoryEntry QWorkspaceSnapshotRead() => new(
        QWorkspaceSegment?.LSegmentListRead() ?? Array.Empty<LPiece>(),
        QWorkspaceSegment?.LSegmentSelectionRead(),
        QWorkspacePreset.LPresetRecordCreate());

    public void QWorkspaceSectionHandle(IReadOnlyList<LPiece> lSections, int? lSectionSelect) => QWorkspaceHistoryAdd();

    public void QWorkspaceHistoryAdd() => qWorkspaceHistory.LHistoryAdd(QWorkspaceSnapshotRead());

    public void QWorkspaceHistoryReset() => qWorkspaceHistory.LHistoryReset(QWorkspaceSnapshotRead());

    public bool QWorkspaceUndo() =>
        QWorkspaceEditableCheck() && QWorkspaceSnapshotApply(qWorkspaceHistory.LHistoryUndo());

    public bool QWorkspaceRedo() =>
        QWorkspaceEditableCheck() && QWorkspaceSnapshotApply(qWorkspaceHistory.LHistoryRedo());

    public void QWorkspacePathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath))
        {
            QWorkspaceMediaClosed?.Invoke();
        }
    }

    public void QWorkspaceRemovedHandle(IReadOnlyList<string> lRemovedPaths)
    {
        (bool lCancelled, bool lClosed) = qWorkspaceGate.CWorkspaceRemovalRead(lRemovedPaths);
        if (lCancelled)
        {
            QWorkspaceLoadCancel();
        }

        if (lClosed)
        {
            QWorkspaceMediaClosed?.Invoke();
        }
    }

    public bool QWorkspaceMediaClear(IReadOnlySet<Guid> lCohorts)
    {
        string[] lKept = QWorkspaceKeptRead(lCohorts);
        (bool lClosed, bool lCancelled, bool lCleared) = qWorkspaceGate.CWorkspaceClearRead(lKept);
        if (lClosed)
        {
            QWorkspaceMediaClosed?.Invoke();
        }

        if (lCancelled)
        {
            QWorkspaceLoadCancel();
        }

        int lStaleRemoved = QWorkspaceDocket?.LDocketStaleRemove(lCohorts) ?? 0;
        return lCleared || lStaleRemoved > 0;
    }

    private string[] QWorkspaceKeptRead(IReadOnlySet<Guid> lCohorts) =>
        QWorkspaceDocket?.LDocketProtectedRead(lCohorts)
            .Select(lEntry => lEntry.LDocketEntryPath)
            .ToArray() ?? [];

    private void QWorkspaceMediaHandle(LCargo lCargo)
    {
        QWorkspaceLosslesscutHandle(lCargo);
        QWorkspaceFlowHandle(lCargo);
        QWorkspaceRelay.QWorkspaceRelayHandle(lCargo);
    }

    private void QWorkspaceFlowHandle(LCargo lCargo)
    {
        if (QWorkspaceFlow is null)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            QWorkspaceFlowAttached?.Invoke(lMediaInfo, lCargo.LCargoSourcePath);
            return;
        }

        QWorkspaceFlowCleared?.Invoke();
    }

    private void QWorkspaceLosslesscutHandle(LCargo lCargo)
    {
        if (QWorkspaceFlow is not { } lFlow || !QWorkspaceFacts.QWorkspaceSectionVisible)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is null || string.IsNullOrWhiteSpace(lCargo.LCargoSourcePath))
        {
            lFlow.QFlowStamp.QFlowLosslesscutSet(string.Empty);
            return;
        }

        if (lFlow.QFlowStamp.QFlowLosslesscutSet(LLosslesscut.LLosslesscutPathNormalize(lCargo.LCargoSourcePath)))
        {
            QWorkspaceLosslesscutFound?.Invoke();
        }
    }

    public void QWorkspacePresetAccept(LPreset lPreset)
    {
        QWorkspacePresetOwner.LPresetSelectionAccept(lPreset.LPresetRecordCreate(), lPreset.LPresetName);
        QWorkspacePresetHandle();
    }

    private void QWorkspacePresetHandle() =>
        QWorkspacePreset.LPresetCopy(LPreset.LPresetStateCreate(QWorkspacePresetOwner.LPresetSelectionValue));

    private bool QWorkspaceEditableCheck() => QWorkspaceFlow is null || QWorkspaceFlow.QFlowEdit.QFlowSectionEditable;

    private bool QWorkspaceSnapshotApply(LHistoryEntry? lEntry)
    {
        if (lEntry is null)
        {
            return false;
        }

        qWorkspaceHistory.LHistorySuspend();
        try
        {
            QWorkspaceSectionsSet(lEntry.LHistorySections, lEntry.LHistorySectionIndex);
            QWorkspacePresetOwner.LPresetSelectionValue = lEntry.LHistoryExport;
        }
        finally
        {
            qWorkspaceHistory.LHistoryResume();
        }

        return true;
    }

    private void QWorkspaceSectionsSet(IReadOnlyList<LPiece> lSections, int? lSectionSelect)
    {
        if (QWorkspaceFlow is not { } lFlow || QWorkspaceSegment is not { } lSegment
            || !lFlow.QFlowEdit.QFlowSectionEditable || lFlow.QFlowRange is null)
        {
            return;
        }

        lSegment.LSegmentBoundSet(lSections, lSectionSelect, lFlow.QFlowDuration);
    }

    private void QWorkspaceLoadCancel() => QWorkspaceViewer?.QViewerMedia.QViewerLoadCancel();
}
