using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QWorkspaceMode
{
    private readonly CUsher qWorkspaceUsher;
    private readonly CInspector qWorkspaceInspector;
    private readonly CEdit qWorkspaceEdit;
    private readonly CFix qWorkspaceFix;
    private readonly CMerge qWorkspaceMerge;
    private readonly CConvert qWorkspaceConvert;
    private readonly LPresetSelection qWorkspacePreset;
    private readonly QViewer? qWorkspaceViewer;
    private readonly QList? qWorkspaceList;
    private readonly QFlow? qWorkspaceFlow;

    // Each tab gate is read from the atelier here, its one reader: a new tab's gate is one field and one line below.
    public QWorkspaceMode(
        QWorkspaceFacts qWorkspaceKey,
        CAtelier lAtelier,
        LPresetSelection lPreset,
        QViewer? lViewer,
        QList? lList,
        QFlow? lFlow)
    {
        qWorkspaceFix = lAtelier.CAtelierFix;
        qWorkspaceMerge = lAtelier.CAtelierMerge;
        qWorkspaceConvert = lAtelier.CAtelierConvert;
        qWorkspaceUsher = lAtelier.CAtelierUsher;
        qWorkspaceInspector = lAtelier.CAtelierInspectorCreate();
        qWorkspaceEdit = lAtelier.CAtelierEdit;
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
        QSplitTab lSplitTab = new(
            qWorkspaceInspector, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!, qWorkspaceFlow!);
        QSplitTabAttach(lSplitTab);
        return lSplitTab;
    }

    private void QSplitTabAttach(QSplitTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += lTab.QSplitRun;
        QWorkspaceAction.QActionAllAdded += lTab.QSplitAllRun;
        QWorkspaceAction.QActionItemsAdded += lTab.QSplitItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
    }

    public QEditTab QEditTabCreate()
    {
        QEditTab lEditTab = new(
            qWorkspaceUsher, qWorkspaceInspector, qWorkspaceEdit, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QEditTabAttach(lEditTab);
        return lEditTab;
    }

    private void QEditTabAttach(QEditTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += lTab.QEditRun;
        QWorkspaceAction.QActionAllAdded += (lRelayTarget, lSourceTab) =>
            lTab.QEditItemsRun(null, lRelayTarget, lSourceTab);
        QWorkspaceAction.QActionItemsAdded += lTab.QEditItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
    }

    public QFixTab QFixTabCreate()
    {
        QFixTab lFixTab = new(
            qWorkspaceInspector, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!, qWorkspaceFix);
        QFixTabAttach(lFixTab);
        return lFixTab;
    }

    private void QFixTabAttach(QFixTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += lTab.QFixRun;
        QWorkspaceAction.QActionAllAdded += lTab.QFixAllRun;
        QWorkspaceAction.QActionItemsAdded += lTab.QFixItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
    }

    public QAudioTab QAudioTabCreate()
    {
        QAudioTab lAudioTab = new(
            qWorkspaceUsher, qWorkspaceInspector, qWorkspaceEdit, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!);
        QAudioTabAttach(lAudioTab);
        return lAudioTab;
    }

    private void QAudioTabAttach(QAudioTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += lTab.QAudioRun;
        QWorkspaceAction.QActionAllAdded += lTab.QAudioAllRun;
        QWorkspaceAction.QActionItemsAdded += lTab.QAudioItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
    }

    public QConvertTab QConvertTabCreate()
    {
        QConvertTab lConvertTab = new(qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!, qWorkspaceConvert);
        QConvertTabAttach(lConvertTab);
        return lConvertTab;
    }

    private void QConvertTabAttach(QConvertTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += lTab.QConvertRun;
        QWorkspaceAction.QActionAllAdded += lTab.QConvertAllRun;
        QWorkspaceAction.QActionItemsAdded += lTab.QConvertItemsRun;
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
    }

    public QMergeTab QMergeTabCreate()
    {
        QMergeTab lMergeTab = new(
            qWorkspaceUsher, qWorkspacePreset, qWorkspaceViewer!, qWorkspaceList!, qWorkspaceMerge);
        QMergeTabAttach(lMergeTab);
        return lMergeTab;
    }

    private void QMergeTabAttach(QMergeTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += lTab.QMergeRun;
        QWorkspaceAction.QActionAllAdded += lTab.QMergeAllRun;
        QWorkspaceAction.QActionCohortAdded += lTab.QMergeCohortRun;
        QWorkspaceAction.QActionEligibleAttach(lTab.QMergeEligibleRead);
    }

    public QFunnelTab QFunnelTabCreate()
    {
        QFunnelTab lFunnelTab = new(qWorkspaceViewer!, qWorkspaceList!);
        QFunnelTabAttach(lFunnelTab);
        return lFunnelTab;
    }

    private void QFunnelTabAttach(QFunnelTab lTab)
    {
        QWorkspaceAction!.QActionExecuted += (_, _, _) => lTab.QFunnelRun();
        QWorkspaceAction.QActionAllAdded += (_, _) => lTab.QFunnelAllRun();
        QWorkspaceAction.QActionItemsAdded += (lPaths, _, _) => lTab.QFunnelItemsRun(lPaths);
        QWorkspaceAction.QActionListAttach(qWorkspaceList!);
        QWorkspaceAction.QActionEligibleAttach(lTab.QFunnelEligibleRead);
    }
}
