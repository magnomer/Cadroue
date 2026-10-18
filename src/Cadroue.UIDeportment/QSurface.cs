using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class QSurface
{
    private const double QSurfaceWidthPadding = 16;
    private readonly QColumnPlan qSurfacePlan;
    private readonly QColumn qSurfaceColumn;
    private readonly int qSurfaceCount;
    private readonly int qSurfaceExportIndex;
    private readonly QAction? qSurfaceAction;

    public QSurface(
        QAction? lAction,
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex,
        int lExportIndex)
    {
        qSurfaceAction = lAction;
        qSurfacePlan = new QColumnPlan(lMinimumWidths, lStoredWidths, lCompactPanels, lFlexIndex);
        qSurfaceColumn = qSurfacePlan.QColumn;
        qSurfaceCount = lMinimumWidths.Count;
        qSurfaceExportIndex = lExportIndex;
    }

    public QColumnPlan QSurfacePlan => qSurfacePlan;

    public int QSurfaceExportIndex => qSurfaceExportIndex;

    public bool QSurfaceExportHidden =>
        qSurfaceExportIndex >= 0 && qSurfaceColumn.QColumnHiddenCheck(qSurfaceExportIndex);

    public bool QSurfaceAutoRelay => qSurfaceAction?.QActionAutoRelay ?? false;

    public bool QSurfaceCollapsedCheck(int lIndex) => qSurfaceColumn.QColumnFixedRead(lIndex) > 0;

    public bool QSurfaceToggleResolve() => !QSurfaceExportHidden;

    public double QSurfaceWidthResolve(double lColumnTotal) => lColumnTotal + QSurfaceWidthPadding;

    public static bool QSurfaceExportResolve(LSceneTabEntry? lLayout) => lLayout?.LSceneExportHidden == true;

    public static bool QSurfaceAutoResolve(LSceneTabEntry? lLayout) => lLayout?.LSceneAutoRelay ?? false;

    public IReadOnlyList<int> QSurfaceCollapsedResolve(LSceneTabEntry? lLayout) =>
        lLayout?.LScenePanelsCollapsed
            .Where(lIndex => lIndex >= 0 && lIndex < qSurfaceCount)
            .ToArray()
        ?? Array.Empty<int>();

    public LSceneTabEntry QSurfaceLayoutRead()
    {
        var lLayout = new LSceneTabEntry
        {
            LSceneExportHidden = QSurfaceExportHidden,
            LSceneAutoRelay = QSurfaceAutoRelay
        };
        for (int lIndex = 0; lIndex < qSurfaceCount; lIndex++)
        {
            if (QSurfaceCollapsedCheck(lIndex))
            {
                lLayout.LScenePanelsCollapsed.Add(lIndex);
            }
        }

        lLayout.LScenePanelWidths.AddRange(qSurfaceColumn.QColumnWeightsRead());
        return lLayout;
    }
}
