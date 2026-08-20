using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PPanels;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;
using Cadroue.ShellEngine;

namespace Cadroue.UIShell.PMainArea;

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
                lPresetOwner,
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
                lPresetOwner,
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
                lPresetOwner,
                pAction.PActionRelayTarget,
                pAction.PActionSourceTab);
        };
        pAction.PActionSelectionSource = () => pList.PListSelectionRead();
        pList.PListPathChange += PConvertPathShow;
        PTabViewerAttach(pList, pViewer, pFlow);
        pViewer.PDropPathsChange += pDropPaths => pList.PListPathsAdd(pDropPaths);
        var pExport = new PExport(lPresetOwner);
        PTabLockAttach(pList, pExport);
        pTabGrid = PTabGridBuild(new System.Windows.UIElement[] { pList, pViewer, pExport }, new PCompass(pFlow), pAction, pFlow, lPreferenceTabLayout);
        Content = pTabGrid;
    }

    private void PConvertPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath))
        {
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    public override PFlowControl PTabFlow => pFlow;
    public override PViewer? PTabViewer => pViewer;
    public override PList? PTabList => pList;
    public override LSceneTabRecord PTabLayoutRead() => PTabLayoutRead(pTabGrid);
}
