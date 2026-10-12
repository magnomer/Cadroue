using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QWorkspaceMode
{
    private readonly CUsher qWorkspaceUsher;
    private readonly LPresetSelection qWorkspacePreset;
    private readonly QViewer? qWorkspaceViewer;
    private readonly QList? qWorkspaceList;
    private readonly QFlow? qWorkspaceFlow;

    public QWorkspaceMode(
        QWorkspaceFacts qWorkspaceKey,
        CUsher lUsher,
        LPresetSelection lPreset,
        QViewer? lViewer,
        QList? lList,
        QFlow? lFlow)
    {
        qWorkspaceUsher = lUsher;
        qWorkspacePreset = lPreset;
        qWorkspaceViewer = lViewer;
        qWorkspaceList = lList;
        qWorkspaceFlow = lFlow;
        QWorkspaceAction = qWorkspaceKey.QWorkspaceWorklist ? null : new QAction();
    }

    public QAction? QWorkspaceAction { get; }

    public QSurface QSurfaceCreate(
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex,
        int lExportIndex) =>
        new(QWorkspaceAction, lMinimumWidths, lStoredWidths, lCompactPanels, lFlexIndex, lExportIndex);

    public QSplitTab QSplitTabCreate()
    {
        QSplitTab lSplitTab = new(qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!, qWorkspaceFlow!);
        QWorkspaceAction!.QActionExecuted += lSplitTab.QSplitRun;
        QWorkspaceAction.QActionAllAdded += lSplitTab.QSplitAllRun;
        QWorkspaceAction.QActionItemsAdded += lSplitTab.QSplitItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        return lSplitTab;
    }

    public QEditTab QEditTabCreate()
    {
        QEditTab lEditTab = new(qWorkspaceUsher, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QWorkspaceAction!.QActionExecuted += lEditTab.QEditRun;
        QWorkspaceAction.QActionAllAdded += lEditTab.QEditAllRun;
        QWorkspaceAction.QActionItemsAdded += lEditTab.QEditItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        return lEditTab;
    }

    public QFixTab QFixTabCreate()
    {
        QFixTab lFixTab = new(qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QWorkspaceAction!.QActionExecuted += lFixTab.QFixRun;
        QWorkspaceAction.QActionAllAdded += lFixTab.QFixAllRun;
        QWorkspaceAction.QActionItemsAdded += lFixTab.QFixItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        return lFixTab;
    }

    public QAudioTab QAudioTabCreate()
    {
        QAudioTab lAudioTab = new(qWorkspaceUsher, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QWorkspaceAction!.QActionExecuted += lAudioTab.QAudioRun;
        QWorkspaceAction.QActionAllAdded += lAudioTab.QAudioAllRun;
        QWorkspaceAction.QActionItemsAdded += lAudioTab.QAudioItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        return lAudioTab;
    }

    public QConvertTab QConvertTabCreate()
    {
        QConvertTab lConvertTab = new(qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QWorkspaceAction!.QActionExecuted += lConvertTab.QConvertRun;
        QWorkspaceAction.QActionAllAdded += lConvertTab.QConvertAllRun;
        QWorkspaceAction.QActionItemsAdded += lConvertTab.QConvertItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        return lConvertTab;
    }

    public QMergeTab QMergeTabCreate()
    {
        QMergeTab lMergeTab = new(qWorkspaceUsher, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QWorkspaceAction!.QActionExecuted += lMergeTab.QMergeRun;
        QWorkspaceAction.QActionAllAdded += lMergeTab.QMergeAllRun;
        QWorkspaceAction.QActionCohortAdded += lMergeTab.QMergeCohortRun;
        QWorkspaceAction.QActionEligibleAttach(lMergeTab.QMergeEligibleRead);
        return lMergeTab;
    }

    public QFunnelTab QFunnelTabCreate()
    {
        QFunnelTab lFunnelTab = new(qWorkspaceViewer!, qWorkspaceList!);
        QWorkspaceAction!.QActionExecuted += (_, _, _) => lFunnelTab.QFunnelRun();
        QWorkspaceAction.QActionAllAdded += (_, _) => lFunnelTab.QFunnelAllRun();
        QWorkspaceAction.QActionItemsAdded += (lPaths, _, _) => lFunnelTab.QFunnelItemsRun(lPaths);
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        QWorkspaceAction.QActionEligibleAttach(lFunnelTab.QFunnelEligibleRead);
        return lFunnelTab;
    }
}
