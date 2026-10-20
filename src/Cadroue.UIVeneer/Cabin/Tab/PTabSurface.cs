using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIVeneer;

public abstract class PTabSurface : UserControl
{
    public abstract PFlow? PTabFlow { get; }
    public abstract PViewer? PTabViewer { get; }
    public virtual PList? PTabList => null;
    public virtual PGroup? PTabGroup => null;
    public QSurface QSurface { get; private set; } = null!;
    public virtual void PTabClose() => PTabList?.PListClose();
    public virtual void PTabStripAttach(QStrip lStrip, QStripTab lStripTab)
    {
    }

    public virtual void PTabTargetsResolve()
    {
    }

    public abstract LSceneTabEntry PTabLayoutRead();

    private PColumn PTabColumn { get; set; } = null!;
    private IReadOnlyList<UIElement> PTabPanels { get; set; } = Array.Empty<UIElement>();
    private UIElement? PTabExportPanel { get; set; }
    private UIElement? PTabExportSplitter { get; set; }
    private ColumnDefinition? PTabExportColumn { get; set; }

    public event Action? PTabWidthChanged;

    public void PTabExportToggle()
    {
        PTabExportApply(QSurface.QSurfaceToggleResolve());
        PTabWidthRaise();
    }

    public virtual double PTabWidthRead() => QSurface.QSurfaceWidthResolve(PTabColumn.PColumnTotalRead());

    protected void PTabWidthRaise() => PTabWidthChanged?.Invoke();

    protected LSceneTabEntry PTabLayoutCreate() => QSurface.QSurfaceLayoutRead();

    protected Grid PTabGridBuild(
        QWorkspaceMode qWorkspaceMode,
        IReadOnlyList<UIElement> pPanels,
        UIElement pCompass,
        PAction pAction,
        UIElement pFlow,
        LSceneTabEntry? lPreferenceTabLayout)
    {
        Grid pGrid = PTabGrid.PTabFrameBuild();
        Grid pPanelGrid = PTabGrid.PTabBodyBuild();
        List<ColumnDefinition> pColumnItems = pPanels.Select(PTabPanel.PTabColumnBuild).ToList();
        List<ColumnDefinition> pSplitterColumns = pPanels.Select(PTabGrid.PTabGutterBuild).ToList();
        IReadOnlyList<int> pPanelIndexes = QSurfaceGrid.QSurfaceIndexesResolve(pPanels.Count);
        IReadOnlyList<int> pSplitterIndexes = QSurfaceGrid.QSurfaceSplittersResolve(pPanels.Count);
        PTabGrid.PTabPanelsAdd(pPanelGrid, pPanels, pColumnItems, pSplitterColumns, pPanelIndexes);

        List<Type> pTypes = pPanels.Select(PTabTypeRead).ToList();
        int pExportIndex = pTypes.LastIndexOf(typeof(PExport));
        PTabSurfaceSet(qWorkspaceMode.QSurfaceCreate(
            pColumnItems.Select(PColumn.PColumnMinimumRead).ToArray(),
            lPreferenceTabLayout?.LScenePanelWidths,
            pPanels.Select(PTabPanel.PTabCompactRead).ToList(),
            pTypes.IndexOf(typeof(PViewer)),
            pExportIndex));
        PTabColumnSet(PColumn.PColumnAttach(pPanelGrid, pColumnItems, QSurface.QSurfacePlan, PTabWidthRaise));
        List<UIElement> pSplitters = pSplitterIndexes
            .Select(PTabColumn.PColumnSplitterBuild)
            .Cast<UIElement>()
            .ToList();
        PTabGrid.PTabSplittersAdd(pPanelGrid, pSplitters, pSplitterIndexes);

        PTabGrid.PTabBodyAdd(pGrid, pPanelGrid);
        PTabPanelsSet(pPanels, pSplitterColumns, pSplitters, pExportIndex);
        pAction.PActionAutoApply(QSurface.QSurfaceAutoResolve(lPreferenceTabLayout));
        PTabExportApply(QSurface.QSurfaceExportResolve(lPreferenceTabLayout));
        pPanelIndexes.ToList().ForEach(PTabCollapseAttach);
        QSurface.QSurfaceCollapsedResolve(lPreferenceTabLayout).ToList().ForEach(PTabCollapseSet);
        PTabGrid.PTabActionAdd(pGrid, pCompass, pAction, pFlow);
        return pGrid;
    }

    private void PTabSurfaceSet(QSurface lSurface) => QSurface = lSurface;

    private void PTabColumnSet(PColumn pColumn) => PTabColumn = pColumn;

    private void PTabPanelsSet(
        IReadOnlyList<UIElement> pPanels,
        List<ColumnDefinition> pSplitterColumns,
        List<UIElement> pSplitters,
        int pExportIndex)
    {
        PTabPanels = pPanels;
        PTabExportPanel = pPanels.ElementAtOrDefault(pExportIndex);
        PTabExportColumn = pSplitterColumns.ElementAtOrDefault(QSurfaceGrid.QSurfaceLeftResolve(pExportIndex));
        PTabExportSplitter = pSplitters.ElementAtOrDefault(QSurfaceGrid.QSurfaceLeftResolve(pExportIndex));
    }

    private void PTabExportApply(bool pHidden)
    {
        PTabColumn.PColumnHiddenSet(QSurface.QSurfaceExportIndex, pHidden);
        PTabExportPanel?.SetValue(VisibilityProperty, PLook.PLookVisible[!pHidden]);
        PTabExportColumn?.SetValue(ColumnDefinition.WidthProperty, PLook.PLookSplitterWidth[pHidden]);
        PTabExportSplitter?.SetValue(VisibilityProperty, PLook.PLookVisible[!pHidden]);
    }

    private void PTabCollapseAttach(int pIndex)
    {
        if (PTabPanel.PTabCollapseRead(PTabPanels[pIndex]) is not { } pCollapse)
        {
            return;
        }

        _ = pCollapse.PTabReader;
        pCollapse.PTabListener(
            pCollapsed => PTabCollapseApply(pIndex, pCollapsed, PTabPanel.PTabStripRead(PTabPanels[pIndex])));
    }

    private void PTabCollapseApply(int pIndex, bool pCollapsed, double pStripWidth)
    {
        PTabColumn.PColumnWidthSet(pIndex, QSurfaceGrid.QSurfaceCollapseResolve(pCollapsed, pStripWidth));
        PTabWidthRaise();
    }

    private void PTabCollapseSet(int pIndex) => PTabPanel.PTabCollapseRead(PTabPanels[pIndex])?.PTabWriter(true);

    private static Type PTabTypeRead(UIElement pPanel) => pPanel.GetType();
}
