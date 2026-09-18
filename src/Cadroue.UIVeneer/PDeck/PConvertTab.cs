using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIVeneer.PDeck;

public sealed class PConvertTab : PTabSurface
{
    private readonly PFlowControl pFlow = new();
    private readonly PViewer pViewer = new();
    private readonly PList pList = new(new LDocket());
    private readonly System.Windows.Controls.Grid pTabGrid;

    public PConvertTab(LPresetSelection lPresetOwner, LSceneTabRecord? lPreferenceTabLayout = null)
    {
        var pAction = new PAction();
        PTabAction = pAction;
        pAction.PActionRun += lPriority =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerConvertDescribe(
                lPriority,
                pList.PListEditableRead() is { } pConvertSelected
                    ? new[] { new LWorkSource(pConvertSelected.LDocketEntryPath, pConvertSelected.LDocketEntryBatch) }
                    : Array.Empty<LWorkSource>(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionAllAdd += () =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerConvertDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionItemsAdd += pConvertPaths =>
        {
            if (!lPresetOwner.LPresetSelectionValid)
            {
                PExport.PExportMissingShow();
                return;
            }

            _ = LMessenger.LMessengerConvertDescribe(
                LWorkPriority.LWorkPriorityNormal,
                pList.PListUnlockedRead()
                    .Where(pItem => pConvertPaths.Contains(pItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                    .Select(pItem => new LWorkSource(pItem.LDocketEntryPath, pItem.LDocketEntryBatch))
                    .ToArray(),
                lPresetOwner.LPresetSelectionEncoding,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionListAttach(pList);
        pList.PListPathChange += PConvertPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => _ = pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(
            new System.Windows.UIElement[] { pList, pViewer, pExport },
            new PCompass(pFlow, pViewer),
            pAction,
            pFlow,
            lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PConvertPathShow(string? pSourcePath) => PTabSourceOpen(pViewer, pSourcePath);

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
