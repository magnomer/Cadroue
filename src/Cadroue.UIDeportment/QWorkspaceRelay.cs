using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QWorkspaceRelay
{
    private readonly QWorkspace qWorkspace;
    private LRelay? qWorkspaceRelayPayload;
    private string qWorkspaceRelayPath = string.Empty;

    public QWorkspaceRelay(QWorkspace lOwner)
    {
        qWorkspace = lOwner;
    }

    public event Action<LRelay, TimeSpan>? QWorkspaceRelayDeferred;
    public event Action<TimeSpan, TimeSpan>? QWorkspaceRangeApplied;
    public event Action<double>? QWorkspaceVolumeApplied;
    public event Action<TimeSpan>? QWorkspaceSeekApplied;
    public event Action<IReadOnlyList<string>>? QWorkspacePathsAdded;
    public event Action<string>? QWorkspaceSourceSelected;
    public event Action<string>? QWorkspaceSourceOpened;

    public bool QWorkspaceRelayPending => qWorkspaceRelayPayload is not null;

    public LRelay QWorkspaceRelayCreate(string lCustomName, double lDropLeft, double lDropTop)
    {
        LRelay lRelay = LRelayPayload.LRelayCreate(
            qWorkspace.QWorkspaceKey,
            lCustomName,
            qWorkspace.QWorkspacePreset.LPresetRecordCreate(),
            qWorkspace.QWorkspaceLayoutRead(),
            lDropLeft,
            lDropTop);
        if (qWorkspace.QWorkspaceDocket is { } lDocket)
        {
            lRelay.LRelayPaths.AddRange(lDocket.LDocketPathsRead());
        }

        if (qWorkspace.QWorkspaceViewer is { } lViewer)
        {
            lRelay.LRelaySourcePath = lViewer.QViewerSourcePath ?? string.Empty;
            lRelay.LRelayPositionTicks = lViewer.QViewerPosition.Ticks;
            lRelay.LRelayVolume = lViewer.QViewerAudio.QViewerVolume;
        }

        if (qWorkspace.QWorkspaceFlow is { } lFlow && qWorkspace.QWorkspaceSegment is { } lSegment)
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
        qWorkspaceRelayPayload =
            qWorkspace.QWorkspaceViewer is not null && !string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath)
                ? lRelay
                : null;
        if (qWorkspace.QWorkspaceDocket is not null && lRelay.LRelayPaths.Count > 0)
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
        if (string.IsNullOrWhiteSpace(lPath) || qWorkspace.QWorkspaceViewer is null)
        {
            return;
        }

        if (qWorkspace.QWorkspaceDocket is { } lDocket
            && lDocket.LDocketPathsRead().Contains(lPath, StringComparer.OrdinalIgnoreCase))
        {
            QWorkspaceSourceSelected?.Invoke(lPath);
            return;
        }

        QWorkspaceSourceOpened?.Invoke(lPath);
    }

    public void QWorkspaceRelayRestore(LRelay lRelay, TimeSpan lDuration)
    {
        if (qWorkspace.QWorkspaceFlow is not null && qWorkspace.QWorkspaceSegment is { } lSegment)
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

    internal void QWorkspaceRelayHandle(LCargo lCargo)
    {
        if (qWorkspaceRelayPayload is not { } lRelay
            || !string.Equals(lCargo.LCargoSourcePath, lRelay.LRelaySourcePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        qWorkspaceRelayPayload = null;
        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            QWorkspaceRelayDeferred?.Invoke(lRelay, lMediaInfo.LMediaInfoDuration);
        }
    }

    internal void QWorkspaceRelayClose() => qWorkspaceRelayPayload = null;
}
