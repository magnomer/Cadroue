using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LWorkspace
{
    private readonly LHistory lWorkspaceHistory = new();
    private readonly QWorkspacePair? qWorkspacePair;
    private Func<LSceneTabEntry>? lWorkspaceLayoutSource;
    private LRelay? lWorkspaceRelay;
    private string lWorkspaceRelayPath = string.Empty;

    public LWorkspace(string lKey, CUsher lUsher, CLineage lLineage)
    {
        LWorkspaceKey = lKey;
        QWorkspaceKey = new QWorkspaceKey(lKey);
        LWorkspaceRoster = QWorkspaceKey.LWorkspaceWorklist ? LRoster.LRosterCreate(lUsher, lLineage) : null;
        LWorkspaceList = QWorkspaceKey.LWorkspaceListPresent ? new LList(lUsher) : null;
        qWorkspacePair = QWorkspaceKey.LWorkspaceFlowPresent ? new QWorkspacePair(QWorkspaceKey, lUsher) : null;
        LWorkspacePreset = LPreset.LPresetInitialCreate(lKey);
        LWorkspacePresetOwner = new LPresetSelection(string.Empty, lKey);
        LWorkspacePresetOwner.LPresetSelectionChanged += LWorkspacePresetHandle;
        LWorkspacePresetAccept(LWorkspacePreset);
        LWorkspaceExport = new LExport(lUsher, LWorkspacePresetOwner, QWorkspaceKey);
        QWorkspaceMode = new QWorkspaceMode(QWorkspaceKey, lUsher,
            LWorkspacePresetOwner, LWorkspaceViewer, LWorkspaceList, LWorkspaceFlow);
    }

    public event Action<LMediaInfo, string>? LWorkspaceFlowAttached;
    public event Action? LWorkspaceFlowCleared;
    public event Action? LWorkspaceMediaClosed;
    public event Action? LWorkspaceLosslesscutFound;
    public event Action<LRelay, TimeSpan>? LWorkspaceRelayDeferred;
    public event Action<TimeSpan, TimeSpan>? LWorkspaceRangeApplied;
    public event Action<double>? LWorkspaceVolumeApplied;
    public event Action<TimeSpan>? LWorkspaceSeekApplied;
    public event Action<IReadOnlyList<string>>? LWorkspacePathsAdded;
    public event Action<string>? LWorkspaceSourceSelected;
    public event Action<string>? LWorkspaceSourceOpened;

    public string LWorkspaceKey { get; }

    public QWorkspaceKey QWorkspaceKey { get; }

    public QWorkspaceMode QWorkspaceMode { get; }

    public LPreset LWorkspacePreset { get; }

    public LPresetSelection LWorkspacePresetOwner { get; }

    public LRoster? LWorkspaceRoster { get; }

    public LExport LWorkspaceExport { get; }

    public LList? LWorkspaceList { get; }

    public LDocket? LWorkspaceDocket => LWorkspaceList?.LListDocket;

    public LViewer? LWorkspaceViewer => qWorkspacePair?.LWorkspaceViewer;

    public LFlow? LWorkspaceFlow => qWorkspacePair?.LWorkspaceFlow;

    public LCompass? LWorkspaceCompass => qWorkspacePair?.LWorkspaceCompass;

    public LSource? LWorkspaceSource => qWorkspacePair?.LWorkspaceSource;

    public LInfo? LWorkspaceInfo => qWorkspacePair?.LWorkspaceInfo;

    public bool LWorkspaceRelayPending => lWorkspaceRelay is not null;

    public bool LWorkspaceFlowPresent => qWorkspacePair is not null;

    private LSegment? LWorkspaceSegment => LWorkspaceFlow?.LFlowSection.LFlowSegment;

    public void LWorkspaceAttach(Func<LSceneTabEntry> lLayoutSource)
    {
        lWorkspaceLayoutSource = lLayoutSource;
        if (LWorkspaceViewer is { } lViewer)
        {
            lViewer.LViewerMediaChanged += LWorkspaceMediaHandle;
        }

        if (LWorkspaceFlow is { } lFlow)
        {
            lFlow.LFlowSection.LFlowSectionChanged += LWorkspaceSectionHandle;
            lFlow.LFlowMediaChanged += LWorkspaceHistoryReset;
        }

        if (LWorkspaceList is { } lList)
        {
            lList.LListPathChanged += LWorkspacePathHandle;
            lList.LListClearChanged += LWorkspaceRemovedHandle;
        }

        qWorkspacePair?.LWorkspacePairAttach();
        lWorkspaceHistory.LHistoryReset(LWorkspaceSnapshotRead());
        LWorkspacePreset.LPresetChanged += LWorkspaceHistoryAdd;
    }

    public void LWorkspaceClose()
    {
        try
        {
            if (LWorkspaceViewer is not null)
            {
                LWorkspaceViewer.LViewerMediaChanged -= LWorkspaceMediaHandle;
            }

            if (LWorkspaceFlow is not null)
            {
                LWorkspaceFlow.LFlowSection.LFlowSectionChanged -= LWorkspaceSectionHandle;
                LWorkspaceFlow.LFlowMediaChanged -= LWorkspaceHistoryReset;
            }

            qWorkspacePair?.LWorkspacePairDetach();
            lWorkspaceRelay = null;
            LWorkspacePreset.LPresetChanged -= LWorkspaceHistoryAdd;
            LWorkspacePresetOwner.LPresetSelectionChanged -= LWorkspacePresetHandle;
            LWorkspacePresetOwner.LPresetSelectionClose();
        }
        finally
        {
            LWorkspaceRoster?.LRosterClose();
        }
    }

    public LStation? LWorkspaceStation => LWorkspaceRoster?.LRosterStation;

    public bool LWorkspaceBusyCheck() => LWorkspaceStation?.LStationBusyCheck() == true;

    public LSceneTabEntry LWorkspaceLayoutRead() => lWorkspaceLayoutSource?.Invoke() ?? new LSceneTabEntry();

    public LHistoryEntry LWorkspaceSnapshotRead() => new(
        LWorkspaceSegment?.LSegmentListRead() ?? Array.Empty<LPiece>(),
        LWorkspaceSegment?.LSegmentSelectionRead(),
        LWorkspacePreset.LPresetRecordCreate());

    public void LWorkspaceSectionHandle(IReadOnlyList<LPiece> lSections, int? lSectionSelect) => LWorkspaceHistoryAdd();

    public void LWorkspaceHistoryAdd() => lWorkspaceHistory.LHistoryAdd(LWorkspaceSnapshotRead());

    public void LWorkspaceHistoryReset() => lWorkspaceHistory.LHistoryReset(LWorkspaceSnapshotRead());

    public bool LWorkspaceUndo() =>
        LWorkspaceEditableCheck() && LWorkspaceSnapshotApply(lWorkspaceHistory.LHistoryUndo());

    public bool LWorkspaceRedo() =>
        LWorkspaceEditableCheck() && LWorkspaceSnapshotApply(lWorkspaceHistory.LHistoryRedo());

    public void LWorkspacePathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath))
        {
            LWorkspaceMediaClosed?.Invoke();
        }
    }

    public void LWorkspaceRemovedHandle(IReadOnlyList<string> lRemovedPaths)
    {
        if (LWorkspaceViewer is null)
        {
            return;
        }

        if (LWorkspaceViewer.LViewerIntent is { } lPending
            && lRemovedPaths.Contains(lPending.LViewerIntentPath, StringComparer.OrdinalIgnoreCase))
        {
            LWorkspaceLoadCancel();
        }

        if (LWorkspaceViewer.LViewerSourcePath is { } lLoaded
            && lRemovedPaths.Contains(lLoaded, StringComparer.OrdinalIgnoreCase))
        {
            LWorkspaceMediaClosed?.Invoke();
        }
    }

    public bool LWorkspaceMediaClear(IReadOnlySet<Guid> lCohorts)
    {
        IReadOnlySet<string> lProtected = LWorkspaceDocket?.LDocketProtectedRead(lCohorts)
                .Select(lEntry => lEntry.LDocketEntryPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool lCleared = false;
        if (!LWorkspaceProtectedCheck(lProtected))
        {
            lCleared |= LWorkspaceLoadedCheck();
            LWorkspaceMediaClosed?.Invoke();
        }
        else if (LWorkspaceViewer?.LViewerIntent is { } lPending && !lProtected.Contains(lPending.LViewerIntentPath))
        {
            LWorkspaceLoadCancel();
            lCleared = true;
        }

        if (LWorkspaceDocket is { } lDocket && lDocket.LDocketPathsRead().Count > 0)
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

    public LRelay LWorkspaceRelayCreate(string lCustomName, double lDropLeft, double lDropTop)
    {
        LRelay lRelay = LRelayPayload.LRelayCreate(
            LWorkspaceKey,
            lCustomName,
            LWorkspacePreset.LPresetRecordCreate(),
            LWorkspaceLayoutRead(),
            lDropLeft,
            lDropTop);
        if (LWorkspaceDocket is { } lDocket)
        {
            lRelay.LRelayPaths.AddRange(lDocket.LDocketPathsRead());
        }

        if (LWorkspaceViewer is { } lViewer)
        {
            lRelay.LRelaySourcePath = lViewer.LViewerSourcePath ?? string.Empty;
            lRelay.LRelayPositionTicks = lViewer.LViewerPosition.Ticks;
            lRelay.LRelayVolume = lViewer.LViewerVolume;
        }

        if (LWorkspaceFlow is { } lFlow && LWorkspaceSegment is { } lSegment)
        {
            lRelay.LRelaySections = LRelayPayload.LRelayRecordsCreate(lSegment.LSegmentListRead());
            lRelay.LRelaySectionIndex = lSegment.LSegmentSelectionRead();
            if (lFlow.LFlowSpool is { } lSpool)
            {
                lRelay.LRelayOriginTicks = lSpool.LSpoolRangeOrigin.Ticks;
                lRelay.LRelayLimitTicks = lSpool.LSpoolRangeLimit.Ticks;
            }
        }

        return lRelay;
    }

    public void LWorkspaceRelayApply(LRelay lRelay)
    {
        lWorkspaceRelayPath = lRelay.LRelaySourcePath;
        lWorkspaceRelay = LWorkspaceViewer is not null && !string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath)
            ? lRelay
            : null;
        if (LWorkspaceDocket is not null && lRelay.LRelayPaths.Count > 0)
        {
            LWorkspacePathsAdded?.Invoke(lRelay.LRelayPaths);
            return;
        }

        LWorkspaceSourceRun();
    }

    public void LWorkspaceSourceRun()
    {
        string lPath = lWorkspaceRelayPath;
        lWorkspaceRelayPath = string.Empty;
        if (string.IsNullOrWhiteSpace(lPath) || LWorkspaceViewer is null)
        {
            return;
        }

        if (LWorkspaceDocket is { } lDocket
            && lDocket.LDocketPathsRead().Contains(lPath, StringComparer.OrdinalIgnoreCase))
        {
            LWorkspaceSourceSelected?.Invoke(lPath);
            return;
        }

        LWorkspaceSourceOpened?.Invoke(lPath);
    }

    public void LWorkspaceRelayRestore(LRelay lRelay, TimeSpan lDuration)
    {
        if (LWorkspaceFlow is not null && LWorkspaceSegment is { } lSegment)
        {
            IReadOnlyList<LPiece> lSections = LRelayPayload.LRelaySegmentsCreate(lRelay.LRelaySections);
            if (lSections.Count > 0)
            {
                lSegment.LSegmentBoundSet(lSections, lRelay.LRelaySectionIndex, lDuration);
            }

            if (lRelay.LRelayOriginTicks is { } lOrigin && lRelay.LRelayLimitTicks is { } lLimit)
            {
                LWorkspaceRangeApplied?.Invoke(TimeSpan.FromTicks(lOrigin), TimeSpan.FromTicks(lLimit));
            }
        }

        if (lRelay.LRelayVolume is { } lVolume)
        {
            LWorkspaceVolumeApplied?.Invoke(lVolume);
        }

        if (lRelay.LRelayPositionTicks > 0)
        {
            LWorkspaceSeekApplied?.Invoke(TimeSpan.FromTicks(lRelay.LRelayPositionTicks));
        }
    }

    private void LWorkspaceMediaHandle(LCargo lCargo)
    {
        LWorkspaceLosslesscutHandle(lCargo);
        LWorkspaceFlowHandle(lCargo);
        LWorkspaceRelayHandle(lCargo);
    }

    private void LWorkspaceFlowHandle(LCargo lCargo)
    {
        if (LWorkspaceFlow is null)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            LWorkspaceFlowAttached?.Invoke(lMediaInfo, lCargo.LCargoSourcePath);
            return;
        }

        LWorkspaceFlowCleared?.Invoke();
    }

    private void LWorkspaceLosslesscutHandle(LCargo lCargo)
    {
        if (LWorkspaceFlow is not { } lFlow || !QWorkspaceKey.LWorkspaceSectionVisible)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is null || string.IsNullOrWhiteSpace(lCargo.LCargoSourcePath))
        {
            lFlow.QFlowStamp.LFlowLosslesscutSet(string.Empty);
            return;
        }

        if (lFlow.QFlowStamp.LFlowLosslesscutSet(LLosslesscut.LLosslesscutPathNormalize(lCargo.LCargoSourcePath)))
        {
            LWorkspaceLosslesscutFound?.Invoke();
        }
    }

    private void LWorkspaceRelayHandle(LCargo lCargo)
    {
        if (lWorkspaceRelay is not { } lRelay
            || !string.Equals(lCargo.LCargoSourcePath, lRelay.LRelaySourcePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lWorkspaceRelay = null;
        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            LWorkspaceRelayDeferred?.Invoke(lRelay, lMediaInfo.LMediaInfoDuration);
        }
    }

    public void LWorkspacePresetAccept(LPreset lPreset)
    {
        LWorkspacePresetOwner.LPresetSelectionAccept(lPreset.LPresetRecordCreate(), lPreset.LPresetName);
        LWorkspacePresetHandle();
    }

    private void LWorkspacePresetHandle() =>
        LWorkspacePreset.LPresetCopy(LPreset.LPresetStateCreate(LWorkspacePresetOwner.LPresetSelectionValue));

    private bool LWorkspaceEditableCheck() => LWorkspaceFlow is null || LWorkspaceFlow.QFlowEdit.LFlowSectionEditable;

    private bool LWorkspaceSnapshotApply(LHistoryEntry? lEntry)
    {
        if (lEntry is null)
        {
            return false;
        }

        lWorkspaceHistory.LHistorySuspend();
        try
        {
            LWorkspaceSectionsSet(lEntry.LHistorySections, lEntry.LHistorySectionIndex);
            LWorkspacePresetOwner.LPresetSelectionValue = lEntry.LHistoryExport;
        }
        finally
        {
            lWorkspaceHistory.LHistoryResume();
        }

        return true;
    }

    private void LWorkspaceSectionsSet(IReadOnlyList<LPiece> lSections, int? lSectionSelect)
    {
        if (LWorkspaceFlow is not { } lFlow || LWorkspaceSegment is not { } lSegment
            || !lFlow.QFlowEdit.LFlowSectionEditable || lFlow.LFlowSpool is null)
        {
            return;
        }

        lSegment.LSegmentBoundSet(lSections, lSectionSelect, lFlow.LFlowDuration);
    }

    private bool LWorkspaceProtectedCheck(IReadOnlySet<string> lProtected) =>
        LWorkspaceViewer is { } lViewer
        && ((lViewer.LViewerSourcePath is { } lSource && lProtected.Contains(lSource))
            || (lViewer.LViewerIntent is { } lPending && lProtected.Contains(lPending.LViewerIntentPath)));

    private bool LWorkspaceLoadedCheck() =>
        LWorkspaceViewer is { LViewerUnloaded: false } lViewer
        && (lViewer.LViewerSourcePath is not null
            || lViewer.LViewerMediaInfo is not null
            || lViewer.LViewerIntent is not null);

    private void LWorkspaceLoadCancel() => LWorkspaceViewer?.LViewerMedia.LViewerLoadCancel();
}
