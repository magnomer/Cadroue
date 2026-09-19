using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LWorkspace
{
    private const string LWorkspaceSplitKey = "Split";
    private const string LWorkspaceAudioKey = "Audio";
    private const string LWorkspaceMergeKey = "Merge";
    private const string LWorkspaceWorklistKey = "Worklist";
    private readonly LHistory lWorkspaceHistory = new();
    private LDocket? lWorkspaceDocket;
    private LSegment? lWorkspaceSegment;
    private LFlow? lWorkspaceFlow;
    private LViewer? lWorkspaceViewer;
    private LStation? lWorkspaceStation;
    private Func<LSceneTabRecord>? lWorkspaceLayoutSource;
    private LRelay? lWorkspaceRelay;
    private string lWorkspaceRelayPath = string.Empty;

    public LWorkspace(string lKey, LPreset? lPreset)
    {
        LWorkspaceKey = lKey;
        LWorkspacePreset = lPreset ?? LPreset.LPresetInitialCreate(lKey);
        LWorkspacePresetOwner = new LPresetSelection(
            LWorkspacePreset.LPresetRecordCreate(), LWorkspacePreset.LPresetName);
        LWorkspacePresetOwner.LPresetSelectionChange += LWorkspacePresetHandle;
        LWorkspacePresetHandle();
    }

    public event Action<LMediaInfo, string>? LWorkspaceFlowAttach;
    public event Action? LWorkspaceFlowClear;
    public event Action? LWorkspaceMediaClose;
    public event Action? LWorkspaceLosslesscutFind;
    public event Action<LRelay, TimeSpan>? LWorkspaceRelayDefer;
    public event Action<TimeSpan, TimeSpan>? LWorkspaceRangeApply;
    public event Action<double>? LWorkspaceVolumeApply;
    public event Action<TimeSpan>? LWorkspaceSeekApply;
    public event Action<IReadOnlyList<string>>? LWorkspacePathsAdd;
    public event Action<string>? LWorkspaceSourceSelect;
    public event Action<string>? LWorkspaceSourceOpen;

    public string LWorkspaceKey { get; }

    public LPreset LWorkspacePreset { get; }

    public LPresetSelection LWorkspacePresetOwner { get; }

    public LDocket? LWorkspaceDocket => lWorkspaceDocket;

    public LViewer? LWorkspaceViewer => lWorkspaceViewer;

    public LFlow? LWorkspaceFlow => lWorkspaceFlow;

    public bool LWorkspaceSourcePresent =>
        LWorkspaceKey is not (LWorkspaceMergeKey or LWorkspaceWorklistKey);

    public bool LWorkspaceAudioOnly => string.Equals(LWorkspaceKey, LWorkspaceAudioKey, StringComparison.Ordinal);

    public bool LWorkspaceSectionVisible => string.Equals(LWorkspaceKey, LWorkspaceSplitKey, StringComparison.Ordinal);

    public bool LWorkspaceRelayPending => lWorkspaceRelay is not null;

    public bool LWorkspaceFlowPresent => lWorkspaceFlow is not null;

    public bool LWorkspaceListPresent => lWorkspaceDocket is not null;

    public void LWorkspaceAttach(
        LDocket? lDocket,
        LSegment? lSegment,
        LFlow? lFlow,
        LViewer? lViewer,
        LStation? lStation,
        Func<LSceneTabRecord> lLayoutSource)
    {
        lWorkspaceDocket = lDocket;
        lWorkspaceSegment = lSegment;
        lWorkspaceFlow = lFlow;
        lWorkspaceViewer = lViewer;
        lWorkspaceStation = lStation;
        lWorkspaceLayoutSource = lLayoutSource;
        if (lViewer is not null)
        {
            lViewer.LViewerMediaChange += LWorkspaceMediaHandle;
        }

        lWorkspaceHistory.LHistoryReset(LWorkspaceSnapshotRead());
        LWorkspacePreset.LPresetChange += LWorkspaceHistoryAdd;
    }

    public void LWorkspaceClose()
    {
        if (lWorkspaceViewer is not null)
        {
            lWorkspaceViewer.LViewerMediaChange -= LWorkspaceMediaHandle;
        }

        lWorkspaceRelay = null;
        LWorkspacePreset.LPresetChange -= LWorkspaceHistoryAdd;
        LWorkspacePresetOwner.LPresetSelectionChange -= LWorkspacePresetHandle;
        LWorkspacePresetOwner.LPresetSelectionClose();
    }

    public bool LWorkspaceBusyCheck() => lWorkspaceStation?.LStationBusyCheck() == true;

    public LSceneTabRecord LWorkspaceLayoutRead() => lWorkspaceLayoutSource?.Invoke() ?? new LSceneTabRecord();

    public LHistoryEntry LWorkspaceSnapshotRead() => new(
        lWorkspaceSegment?.LSegmentListRead() ?? Array.Empty<LPiece>(),
        lWorkspaceSegment?.LSegmentSelectionRead(),
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
            LWorkspaceMediaClose?.Invoke();
        }
    }

    public void LWorkspaceRemovedHandle(IReadOnlyList<string> lRemovedPaths)
    {
        if (lWorkspaceViewer is null)
        {
            return;
        }

        if (lWorkspaceViewer.LViewerIntent is { } lPending
            && lRemovedPaths.Contains(lPending.LViewerIntentPath, StringComparer.OrdinalIgnoreCase))
        {
            LWorkspaceLoadCancel();
        }

        if (lWorkspaceViewer.LViewerSourcePath is { } lLoaded
            && lRemovedPaths.Contains(lLoaded, StringComparer.OrdinalIgnoreCase))
        {
            LWorkspaceMediaClose?.Invoke();
        }
    }

    public bool LWorkspaceMediaClear(IReadOnlySet<Guid> lCohorts)
    {
        IReadOnlySet<string> lProtected = lWorkspaceDocket?.LDocketProtectedRead(lCohorts)
                .Select(lEntry => lEntry.LDocketEntryPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool lCleared = false;
        if (!LWorkspaceProtectedCheck(lProtected))
        {
            lCleared |= LWorkspaceLoadedCheck();
            LWorkspaceMediaClose?.Invoke();
        }
        else if (lWorkspaceViewer?.LViewerIntent is { } lPending && !lProtected.Contains(lPending.LViewerIntentPath))
        {
            LWorkspaceLoadCancel();
            lCleared = true;
        }

        if (lWorkspaceDocket is { } lDocket && lDocket.LDocketPathsRead().Count > 0)
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
        if (lWorkspaceDocket is { } lDocket)
        {
            lRelay.LRelayPaths.AddRange(lDocket.LDocketPathsRead());
        }

        if (lWorkspaceViewer is { } lViewer)
        {
            lRelay.LRelaySourcePath = lViewer.LViewerSourcePath ?? string.Empty;
            lRelay.LRelayPositionTicks = lViewer.LViewerPosition.Ticks;
            lRelay.LRelayVolume = lViewer.LViewerVolume;
        }

        if (lWorkspaceFlow is { } lFlow && lWorkspaceSegment is { } lSegment)
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
        lWorkspaceRelay = lWorkspaceViewer is not null && !string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath)
            ? lRelay
            : null;
        if (lWorkspaceDocket is not null && lRelay.LRelayPaths.Count > 0)
        {
            LWorkspacePathsAdd?.Invoke(lRelay.LRelayPaths);
            return;
        }

        LWorkspaceSourceRun();
    }

    public void LWorkspaceSourceRun()
    {
        string lPath = lWorkspaceRelayPath;
        lWorkspaceRelayPath = string.Empty;
        if (string.IsNullOrWhiteSpace(lPath) || lWorkspaceViewer is null)
        {
            return;
        }

        if (lWorkspaceDocket is { } lDocket
            && lDocket.LDocketPathsRead().Contains(lPath, StringComparer.OrdinalIgnoreCase))
        {
            LWorkspaceSourceSelect?.Invoke(lPath);
            return;
        }

        LWorkspaceSourceOpen?.Invoke(lPath);
    }

    public void LWorkspaceRelayRestore(LRelay lRelay, TimeSpan lDuration)
    {
        if (lWorkspaceFlow is not null && lWorkspaceSegment is { } lSegment)
        {
            IReadOnlyList<LPiece> lSections = LRelayPayload.LRelaySegmentsCreate(lRelay.LRelaySections);
            if (lSections.Count > 0)
            {
                lSegment.LSegmentBoundSet(lSections, lRelay.LRelaySectionIndex, lDuration);
            }

            if (lRelay.LRelayOriginTicks is { } lOrigin && lRelay.LRelayLimitTicks is { } lLimit)
            {
                LWorkspaceRangeApply?.Invoke(TimeSpan.FromTicks(lOrigin), TimeSpan.FromTicks(lLimit));
            }
        }

        if (lRelay.LRelayVolume is { } lVolume)
        {
            LWorkspaceVolumeApply?.Invoke(lVolume);
        }

        if (lRelay.LRelayPositionTicks > 0)
        {
            LWorkspaceSeekApply?.Invoke(TimeSpan.FromTicks(lRelay.LRelayPositionTicks));
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
        if (lWorkspaceFlow is null)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            LWorkspaceFlowAttach?.Invoke(lMediaInfo, lCargo.LCargoSourcePath);
            return;
        }

        LWorkspaceFlowClear?.Invoke();
    }

    private void LWorkspaceLosslesscutHandle(LCargo lCargo)
    {
        if (lWorkspaceFlow is not { } lFlow || !LWorkspaceSectionVisible)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is null || string.IsNullOrWhiteSpace(lCargo.LCargoSourcePath))
        {
            lFlow.LFlowLosslesscutSet(string.Empty);
            return;
        }

        if (lFlow.LFlowLosslesscutSet(LLosslesscut.LLosslesscutPathNormalize(lCargo.LCargoSourcePath)))
        {
            LWorkspaceLosslesscutFind?.Invoke();
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
            LWorkspaceRelayDefer?.Invoke(lRelay, lMediaInfo.LMediaInfoDuration);
        }
    }

    private void LWorkspacePresetHandle() =>
        LWorkspacePreset.LPresetCopy(LPreset.LPresetStateCreate(LWorkspacePresetOwner.LPresetSelectionValue));

    private bool LWorkspaceEditableCheck() => lWorkspaceFlow is null || lWorkspaceFlow.LFlowSectionEditable;

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
        if (lWorkspaceFlow is not { } lFlow || lWorkspaceSegment is not { } lSegment
            || !lFlow.LFlowSectionEditable || lFlow.LFlowSpool is null)
        {
            return;
        }

        lSegment.LSegmentBoundSet(lSections, lSectionSelect, lFlow.LFlowDuration);
    }

    private bool LWorkspaceProtectedCheck(IReadOnlySet<string> lProtected) =>
        lWorkspaceViewer is { } lViewer
        && ((lViewer.LViewerSourcePath is { } lSource && lProtected.Contains(lSource))
            || (lViewer.LViewerIntent is { } lPending && lProtected.Contains(lPending.LViewerIntentPath)));

    private bool LWorkspaceLoadedCheck() =>
        lWorkspaceViewer is { LViewerUnloaded: false } lViewer
        && (lViewer.LViewerSourcePath is not null
            || lViewer.LViewerMediaInfo is not null
            || lViewer.LViewerIntent is not null);

    private void LWorkspaceLoadCancel()
    {
        lWorkspaceViewer?.LViewerIntentSet(null);
        lWorkspaceViewer?.LViewerSerialChange();
    }
}
