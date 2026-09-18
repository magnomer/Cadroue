using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIVeneer.PPanel;

namespace Cadroue.UIVeneer.PToolbar;

public sealed partial class PWorkspace
{
    private LRelay? pWorkspaceRelay;

    public LRelay PWorkspaceRelayCreate(PTabRecord pTabRecord, double pDropLeft, double pDropTop)
    {
        LRelay lRelay = LRelayPayload.LRelayCreate(
            pTabRecord.PTabLayoutKey,
            pTabRecord.PTabNameCustom,
            PWorkspaceExportState.LPresetRecordCreate(),
            PWorkspaceLayoutRead(),
            pDropLeft,
            pDropTop);
        if (PWorkspaceSurface.PTabList is { } pList)
        {
            lRelay.LRelayPaths.AddRange(pList.PListPathsRead());
        }

        if (PWorkspaceViewer is { } pViewer)
        {
            lRelay.LRelaySourcePath = pViewer.PViewerSourcePath ?? string.Empty;
            lRelay.LRelayPositionTicks = pViewer.PViewerPositionRead().Ticks;
            lRelay.LRelayVolume = pViewer.PViewerVolumeCurrent;
        }

        if (PWorkspaceFlow is { } pFlow)
        {
            LSegment lRelaySegment = pFlow.PFlowSegment;
            lRelay.LRelaySections = LRelayPayload.LRelayRecordsCreate(lRelaySegment.LSegmentListRead());
            lRelay.LRelaySectionIndex = lRelaySegment.LSegmentSelectionRead();
            if (pFlow.PFlowRangeRead() is var (lRelayOrigin, lRelayLimit))
            {
                lRelay.LRelayOriginTicks = lRelayOrigin.Ticks;
                lRelay.LRelayLimitTicks = lRelayLimit.Ticks;
            }
        }

        return lRelay;
    }

    public async void PWorkspaceRelayApply(LRelay lRelay)
    {
        if (PWorkspaceViewer is { } pRelayViewer && !string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath))
        {
            pRelayViewer.LViewer.LViewerMediaChange += PWorkspaceRelayHandle;
            pWorkspaceRelay = lRelay;
        }

        PList? pRelayList = PWorkspaceSurface.PTabList;
        if (pRelayList is not null && lRelay.LRelayPaths.Count > 0)
        {
            await pRelayList.PListPathsAdd(lRelay.LRelayPaths);
        }

        if (string.IsNullOrWhiteSpace(lRelay.LRelaySourcePath) || PWorkspaceViewer is null)
        {
            return;
        }

        if (pRelayList is not null
            && pRelayList.PListPathsRead().Contains(lRelay.LRelaySourcePath, StringComparer.OrdinalIgnoreCase))
        {
            pRelayList.PListSelect(lRelay.LRelaySourcePath);
            return;
        }

        PWorkspaceViewer.PViewerSourceOpen(lRelay.LRelaySourcePath);
    }

    private void PWorkspaceRelayHandle(LCargo lMediaStatus)
    {
        if (pWorkspaceRelay is not { } lRelay
            || PWorkspaceViewer is not { } pRelayViewer
            || !string.Equals(
                lMediaStatus.LCargoSourcePath,
                lRelay.LRelaySourcePath,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        PWorkspaceRelayDetach();
        if (lMediaStatus.LCargoMediaInfo is not { } lRelayMedia)
        {
            return;
        }

        System.Windows.Application.Current?.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => PWorkspaceRelayRestore(lRelay, pRelayViewer, lRelayMedia.LMediaInfoDuration)));
    }

    private void PWorkspaceRelayDetach()
    {
        if (PWorkspaceViewer is not null)
        {
            PWorkspaceViewer.LViewer.LViewerMediaChange -= PWorkspaceRelayHandle;
        }

        pWorkspaceRelay = null;
    }

    private void PWorkspaceRelayRestore(LRelay lRelay, PViewer pRelayViewer, TimeSpan lRelayDuration)
    {
        if (PWorkspaceFlow is { } pRelayFlow)
        {
            IReadOnlyList<LPiece> lRelaySections = LRelayPayload.LRelaySegmentsCreate(lRelay.LRelaySections);
            if (lRelaySections.Count > 0)
            {
                pRelayFlow.PFlowSegment.LSegmentBoundSet(lRelaySections, lRelay.LRelaySectionIndex, lRelayDuration);
            }

            if (lRelay.LRelayOriginTicks is { } lRelayOrigin && lRelay.LRelayLimitTicks is { } lRelayLimit)
            {
                pRelayFlow.PFlowRangeSet(TimeSpan.FromTicks(lRelayOrigin), TimeSpan.FromTicks(lRelayLimit));
            }
        }

        if (lRelay.LRelayVolume is { } lRelayVolume)
        {
            pRelayViewer.PViewerVolumeSet(lRelayVolume);
        }

        if (lRelay.LRelayPositionTicks > 0)
        {
            pRelayViewer.PViewerSeek(TimeSpan.FromTicks(lRelay.LRelayPositionTicks));
        }
    }
}
