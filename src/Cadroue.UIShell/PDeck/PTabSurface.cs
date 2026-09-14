using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PDeck;

public abstract partial class PTabSurface : UserControl
{
    public abstract PFlowControl? PTabFlow { get; }
    public abstract PViewer? PTabViewer { get; }
    public virtual PList? PTabList => null;
    public virtual PGroup? PTabGroup => null;
    public virtual bool PTabSectionVisible => false;
    public PAction? PTabAction { get; protected set; }
    public virtual bool PTabBusyCheck() => false;
    public virtual void PTabClose() => PTabList?.PListClose();
    public abstract LSceneTabRecord PTabLayoutRead();

    protected const double PTabWidthPadding = 16;

    public event Action? PTabWidthChange;

    public void PTabExportToggle()
    {
        if (PTabStateRead() is { } pState)
        {
            pState.PExportToggle();
            PTabWidthRaise();
        }
    }

    public virtual double PTabWidthRead() =>
        PTabStateRead() is { } pState ? pState.PTabLayout.PColumnTotalRead() + PTabWidthPadding : 0;

    protected void PTabWidthRaise() => PTabWidthChange?.Invoke();

    protected static void PTabViewerAttach(PList pList, PViewer pViewer, PFlowControl pFlow)
    {
        void pTabViewerDetach()
        {
            pViewer.PViewerMediaClose(true);
            pFlow.PFlowClear();
        }

        pList.PListPathChange += pCurrentPath =>
        {
            if (string.IsNullOrWhiteSpace(pCurrentPath))
            {
                pTabViewerDetach();
            }
        };
        pList.PListClearChange += pRemovedPaths =>
        {
            if (pViewer.PViewerPendingPath is { } pPendingPath
                && pRemovedPaths.Any(
                    pRemoved => string.Equals(pRemoved, pPendingPath, StringComparison.OrdinalIgnoreCase)))
            {
                pViewer.PViewerLoadCancel();
            }

            if (pViewer.PViewerSourcePath is { } pLoadedPath
                && pRemovedPaths.Any(
                    pRemoved => string.Equals(pRemoved, pLoadedPath, StringComparison.OrdinalIgnoreCase)))
            {
                pTabViewerDetach();
            }
        };
    }

    protected static void PTabLockAttach(PList pList, params UIElement[] pEditors)
    {
        void pTabLockApply(bool pLocked)
        {
            foreach (UIElement pEditor in pEditors)
            {
                pEditor.IsEnabled = !pLocked;
            }
        }

        pList.PListLockChange += pTabLockApply;
        pTabLockApply(pList.PListLockCheck());
    }

    protected static LSceneTabRecord PTabLayoutRead(Grid pGrid)
    {
        var lPreferenceTabLayout = new LSceneTabRecord();
        if (pGrid.Tag is not PTabGridState pState)
        {
            return lPreferenceTabLayout;
        }

        lPreferenceTabLayout.LSceneExportHidden = pState.PExportHidden;
        lPreferenceTabLayout.LSceneAutoRelay = pState.PTabAction?.PActionAutoRelay ?? false;
        for (int index = 0; index < pState.PTabPanels.Count; index++)
        {
            if (PTabCollapseCheck(pState.PTabPanels[index]))
            {
                lPreferenceTabLayout.LScenePanelsCollapsed.Add(index);
            }
        }

        foreach (double pWeight in pState.PTabLayout.PColumnWeightsRead())
        {
            lPreferenceTabLayout.LScenePanelWidths.Add(pWeight);
        }

        return lPreferenceTabLayout;
    }

    private PTabGridState? PTabStateRead()
    {
        return Content is Grid pGrid && pGrid.Tag is PTabGridState pState ? pState : null;
    }
}
