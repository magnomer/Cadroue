using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QWorkspace
{
    private readonly LHistory qWorkspaceHistory = new();
    private readonly QWorkspacePair? qWorkspacePair;
    private Func<LSceneTabEntry>? qWorkspaceLayoutSource;
    private LRelay? qWorkspaceRelay;
    private string qWorkspaceRelayPath = string.Empty;

    public QWorkspace(string lKey, CUsher lUsher, CLineage lLineage)
    {
        QWorkspaceKey = lKey;
        QWorkspaceFacts = new QWorkspaceFacts(lKey);
        QWorkspaceRoster = QWorkspaceFacts.QWorkspaceWorklist ? QRoster.QRosterCreate(lUsher, lLineage) : null;
        QWorkspaceList = QWorkspaceFacts.QWorkspaceListPresent ? new QList(lUsher) : null;
        qWorkspacePair = QWorkspaceFacts.QWorkspaceFlowPresent ? new QWorkspacePair(QWorkspaceFacts, lUsher) : null;
        QWorkspacePreset = LPreset.LPresetInitialCreate(lKey);
        QWorkspacePresetOwner = new LPresetSelection(string.Empty, lKey);
        QWorkspacePresetOwner.LPresetSelectionChanged += QWorkspacePresetHandle;
        QWorkspacePresetAccept(QWorkspacePreset);
        QWorkspaceExport = new QExport(lUsher, QWorkspacePresetOwner, QWorkspaceFacts);
        QWorkspaceMode = new QWorkspaceMode(QWorkspaceFacts, lUsher,
            QWorkspacePresetOwner, QWorkspaceViewer, QWorkspaceList, QWorkspaceFlow);
    }

    public event Action<LMediaInfo, string>? QWorkspaceFlowAttached;
    public event Action? QWorkspaceFlowCleared;
    public event Action? QWorkspaceMediaClosed;
    public event Action? QWorkspaceLosslesscutFound;
    public event Action<LRelay, TimeSpan>? QWorkspaceRelayDeferred;
    public event Action<TimeSpan, TimeSpan>? QWorkspaceRangeApplied;
    public event Action<double>? QWorkspaceVolumeApplied;
    public event Action<TimeSpan>? QWorkspaceSeekApplied;
    public event Action<IReadOnlyList<string>>? QWorkspacePathsAdded;
    public event Action<string>? QWorkspaceSourceSelected;
    public event Action<string>? QWorkspaceSourceOpened;

    public string QWorkspaceKey { get; }

    public QWorkspaceFacts QWorkspaceFacts { get; }

    public QWorkspaceMode QWorkspaceMode { get; }

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

    public bool QWorkspaceRelayPending => qWorkspaceRelay is not null;

    public bool QWorkspaceFlowPresent => qWorkspacePair is not null;

    private LSegment? QWorkspaceSegment => QWorkspaceFlow?.QFlowSection.QFlowSegment;

    public void QWorkspaceAttach(Func<LSceneTabEntry> lLayoutSource)
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
        qWorkspaceHistory.LHistoryReset(QWorkspaceSnapshotRead());
        QWorkspacePreset.LPresetChanged += QWorkspaceHistoryAdd;
    }

    public void QWorkspaceClose()
    {
        try
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
            qWorkspaceRelay = null;
            QWorkspacePreset.LPresetChanged -= QWorkspaceHistoryAdd;
            QWorkspacePresetOwner.LPresetSelectionChanged -= QWorkspacePresetHandle;
            QWorkspacePresetOwner.LPresetSelectionClose();
        }
        finally
        {
            QWorkspaceRoster?.QRosterClose();
        }
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
        if (QWorkspaceViewer is null)
        {
            return;
        }

        if (QWorkspaceViewer.QViewerIntent is { } lPending
            && lRemovedPaths.Contains(lPending.QViewerIntentPath, StringComparer.OrdinalIgnoreCase))
        {
            QWorkspaceLoadCancel();
        }

        if (QWorkspaceViewer.QViewerSourcePath is { } lLoaded
            && lRemovedPaths.Contains(lLoaded, StringComparer.OrdinalIgnoreCase))
        {
            QWorkspaceMediaClosed?.Invoke();
        }
    }

    public bool QWorkspaceMediaClear(IReadOnlySet<Guid> lCohorts)
    {
        IReadOnlySet<string> lProtected = QWorkspaceDocket?.LDocketProtectedRead(lCohorts)
                .Select(lEntry => lEntry.LDocketEntryPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool lCleared = false;
        if (!QWorkspaceProtectedCheck(lProtected))
        {
            lCleared |= QWorkspaceLoadedCheck();
            QWorkspaceMediaClosed?.Invoke();
        }
        else if (QWorkspaceViewer?.QViewerIntent is { } lPending && !lProtected.Contains(lPending.QViewerIntentPath))
        {
            QWorkspaceLoadCancel();
            lCleared = true;
        }

        if (QWorkspaceDocket is { } lDocket && lDocket.LDocketPathsRead().Count > 0)
        {
            string[] lStale = lDocket.LDocketStaleRead(lCohorts)
                .Select(lEntry => lEntry.LDocketEntryPath)
                .ToArray();
            if (lStale.Length > 0)
            {
                lDocket.LDocketPathsRemove(lStale);
                lCleared = true;
            }
        }

        return lCleared;
    }

    public LRelay QWorkspaceRelayCreate(string lCustomName, double lDropLeft, double lDropTop)
    {
        LRelay lRelay = LRelayPayload.LRelayCreate(
            QWorkspaceKey,
            lCustomName,
            QWorkspacePreset.LPresetRecordCreate(),
            QWorkspaceLayoutRead(),
            lDropLeft,
            lDropTop);
        if (QWorkspaceDocket is { } lDocket)
        {
            lRelay.LRelayPaths.AddRange(lDocket.LDocketPathsRead());
        }

        if (QWorkspaceViewer is { } lViewer)
        {
            lRelay.LRelaySourcePath = lViewer.QViewerSourcePath ?? string.Empty;
            lRelay.LRelayPositionTicks = lViewer.QViewerPosition.Ticks;
            lRelay.LRelayVolume = lViewer.QViewerVolume;
        }

        if (QWorkspaceFlow is { } lFlow && QWorkspaceSegment is { } lSegment)
        {
            lRelay.LRelaySections = LRelayPayload.LRelayRecordsCreate(lSegment.LSegmentListRead());
            lRelay.LRelaySectionIndex = lSegment.LSegmentSelectionRead();
            if (lFlow.QFlowRange is { } lSpool)
            {
                lRelay.LRelayOriginTicks = lSpool.LSpoolRangeOrigin.Ticks;
                lRelay.LRelayLimitTicks = lSpool.LSpoolRangeLimit.Ticks;
            }
        }

        return lRelay;
    }

    public void QWorkspaceRelayApply(LRelay lRelay)
    {
        qWorkspaceRelayPath = lRelay.LRelaySourcePath;
        qWorkspaceRelay = QWorkspaceViewer is not null && !string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath)
            ? lRelay
            : null;
        if (QWorkspaceDocket is not null && lRelay.LRelayPaths.Count > 0)
        {
            QWorkspacePathsAdded?.Invoke(lRelay.LRelayPaths);
            return;
        }

        QWorkspaceSourceRun();
    }

    public void QWorkspaceSourceRun()
    {
        string lPath = qWorkspaceRelayPath;
        qWorkspaceRelayPath = string.Empty;
        if (string.IsNullOrWhiteSpace(lPath) || QWorkspaceViewer is null)
        {
            return;
        }

        if (QWorkspaceDocket is { } lDocket
            && lDocket.LDocketPathsRead().Contains(lPath, StringComparer.OrdinalIgnoreCase))
        {
            QWorkspaceSourceSelected?.Invoke(lPath);
            return;
        }

        QWorkspaceSourceOpened?.Invoke(lPath);
    }

    public void QWorkspaceRelayRestore(LRelay lRelay, TimeSpan lDuration)
    {
        if (QWorkspaceFlow is not null && QWorkspaceSegment is { } lSegment)
        {
            IReadOnlyList<LPiece> lSections = LRelayPayload.LRelaySegmentsCreate(lRelay.LRelaySections);
            if (lSections.Count > 0)
            {
                lSegment.LSegmentBoundSet(lSections, lRelay.LRelaySectionIndex, lDuration);
            }

            if (lRelay.LRelayOriginTicks is { } lOrigin && lRelay.LRelayLimitTicks is { } lLimit)
            {
                QWorkspaceRangeApplied?.Invoke(TimeSpan.FromTicks(lOrigin), TimeSpan.FromTicks(lLimit));
            }
        }

        if (lRelay.LRelayVolume is { } lVolume)
        {
            QWorkspaceVolumeApplied?.Invoke(lVolume);
        }

        if (lRelay.LRelayPositionTicks > 0)
        {
            QWorkspaceSeekApplied?.Invoke(TimeSpan.FromTicks(lRelay.LRelayPositionTicks));
        }
    }

    private void QWorkspaceMediaHandle(LCargo lCargo)
    {
        QWorkspaceLosslesscutHandle(lCargo);
        QWorkspaceFlowHandle(lCargo);
        QWorkspaceRelayHandle(lCargo);
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

    private void QWorkspaceRelayHandle(LCargo lCargo)
    {
        if (qWorkspaceRelay is not { } lRelay
            || !string.Equals(lCargo.LCargoSourcePath, lRelay.LRelaySourcePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        qWorkspaceRelay = null;
        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            QWorkspaceRelayDeferred?.Invoke(lRelay, lMediaInfo.LMediaInfoDuration);
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

    private bool QWorkspaceProtectedCheck(IReadOnlySet<string> lProtected) =>
        QWorkspaceViewer is { } lViewer
        && ((lViewer.QViewerSourcePath is { } lSource && lProtected.Contains(lSource))
            || (lViewer.QViewerIntent is { } lPending && lProtected.Contains(lPending.QViewerIntentPath)));

    private bool QWorkspaceLoadedCheck() =>
        QWorkspaceViewer is { QViewerUnloaded: false } lViewer
        && (lViewer.QViewerSourcePath is not null
            || lViewer.QViewerMediaInfo is not null
            || lViewer.QViewerIntent is not null);

    private void QWorkspaceLoadCancel() => QWorkspaceViewer?.QViewerMedia.QViewerLoadCancel();
}
