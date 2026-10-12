using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QWorkspaceMode
{
    private readonly CUsher lWorkspaceUsher;
    private readonly LPresetSelection lWorkspacePreset;
    private readonly LViewer? lWorkspaceViewer;
    private readonly LList? lWorkspaceList;
    private readonly LFlow? lWorkspaceFlow;

    public QWorkspaceMode(
        QWorkspaceKey qWorkspaceKey,
        CUsher lUsher,
        LPresetSelection lPreset,
        LViewer? lViewer,
        LList? lList,
        LFlow? lFlow)
    {
        lWorkspaceUsher = lUsher;
        lWorkspacePreset = lPreset;
        lWorkspaceViewer = lViewer;
        lWorkspaceList = lList;
        lWorkspaceFlow = lFlow;
        LWorkspaceAction = qWorkspaceKey.LWorkspaceWorklist ? null : new LAction();
    }

    public LAction? LWorkspaceAction { get; }

    public LSurface LSurfaceCreate(
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex,
        int lExportIndex) =>
        new(LWorkspaceAction, lMinimumWidths, lStoredWidths, lCompactPanels, lFlexIndex, lExportIndex);

    public LSplitTab LSplitTabCreate()
    {
        LSplitTab lSplitTab = new(lWorkspacePreset, lWorkspaceViewer!, lWorkspaceList!, lWorkspaceFlow!);
        LWorkspaceAction!.LActionExecuted += lSplitTab.LSplitRun;
        LWorkspaceAction.LActionAllAdded += lSplitTab.LSplitAllRun;
        LWorkspaceAction.LActionItemsAdded += lSplitTab.LSplitItemsRun;
        LWorkspaceAction.LActionListAttach(lWorkspaceList!);
        return lSplitTab;
    }

    public LEditTab LEditTabCreate()
    {
        LEditTab lEditTab = new(lWorkspaceUsher, lWorkspacePreset, lWorkspaceViewer!, lWorkspaceList!);
        LWorkspaceAction!.LActionExecuted += lEditTab.LEditRun;
        LWorkspaceAction.LActionAllAdded += lEditTab.LEditAllRun;
        LWorkspaceAction.LActionItemsAdded += lEditTab.LEditItemsRun;
        LWorkspaceAction.LActionListAttach(lWorkspaceList!);
        return lEditTab;
    }

    public LFixTab LFixTabCreate()
    {
        LFixTab lFixTab = new(lWorkspacePreset, lWorkspaceViewer!, lWorkspaceList!);
        LWorkspaceAction!.LActionExecuted += lFixTab.LFixRun;
        LWorkspaceAction.LActionAllAdded += lFixTab.LFixAllRun;
        LWorkspaceAction.LActionItemsAdded += lFixTab.LFixItemsRun;
        LWorkspaceAction.LActionListAttach(lWorkspaceList!);
        return lFixTab;
    }

    public LAudioTab LAudioTabCreate()
    {
        LAudioTab lAudioTab = new(lWorkspaceUsher, lWorkspacePreset, lWorkspaceViewer!, lWorkspaceList!);
        LWorkspaceAction!.LActionExecuted += lAudioTab.LAudioRun;
        LWorkspaceAction.LActionAllAdded += lAudioTab.LAudioAllRun;
        LWorkspaceAction.LActionItemsAdded += lAudioTab.LAudioItemsRun;
        LWorkspaceAction.LActionListAttach(lWorkspaceList!);
        return lAudioTab;
    }

    public LConvertTab LConvertTabCreate()
    {
        LConvertTab lConvertTab = new(lWorkspacePreset, lWorkspaceViewer!, lWorkspaceList!);
        LWorkspaceAction!.LActionExecuted += lConvertTab.LConvertRun;
        LWorkspaceAction.LActionAllAdded += lConvertTab.LConvertAllRun;
        LWorkspaceAction.LActionItemsAdded += lConvertTab.LConvertItemsRun;
        LWorkspaceAction.LActionListAttach(lWorkspaceList!);
        return lConvertTab;
    }

    public LMergeTab LMergeTabCreate()
    {
        LMergeTab lMergeTab = new(lWorkspaceUsher, lWorkspacePreset, lWorkspaceViewer!, lWorkspaceList!);
        LWorkspaceAction!.LActionExecuted += lMergeTab.LMergeRun;
        LWorkspaceAction.LActionAllAdded += lMergeTab.LMergeAllRun;
        LWorkspaceAction.LActionCohortAdded += lMergeTab.LMergeCohortRun;
        LWorkspaceAction.LActionEligibleAttach(lMergeTab.LMergeEligibleRead);
        return lMergeTab;
    }

    public LFunnelTab LFunnelTabCreate()
    {
        LFunnelTab lFunnelTab = new(lWorkspaceViewer!, lWorkspaceList!);
        LWorkspaceAction!.LActionExecuted += (_, _, _) => lFunnelTab.LFunnelRun();
        LWorkspaceAction.LActionAllAdded += (_, _) => lFunnelTab.LFunnelAllRun();
        LWorkspaceAction.LActionItemsAdded += (lPaths, _, _) => lFunnelTab.LFunnelItemsRun(lPaths);
        LWorkspaceAction.LActionListAttach(lWorkspaceList!);
        LWorkspaceAction.LActionEligibleAttach(lFunnelTab.LFunnelEligibleRead);
        return lFunnelTab;
    }
}
