using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LSurface
{
    private const double LSurfaceWidthPadding = 16;
    private readonly LColumnPlan lSurfacePlan;
    private readonly LColumn lSurfaceColumn;
    private readonly int lSurfaceCount;
    private readonly int lSurfaceExportIndex;
    private readonly LAction? lSurfaceAction;

    public LSurface(
        LAction? lAction,
        IReadOnlyList<double> lMinimumWidths,
        IReadOnlyList<double>? lStoredWidths,
        IReadOnlyList<bool>? lCompactPanels,
        int lFlexIndex,
        int lExportIndex)
    {
        lSurfaceAction = lAction;
        lSurfacePlan = new LColumnPlan(lMinimumWidths, lStoredWidths, lCompactPanels, lFlexIndex);
        lSurfaceColumn = lSurfacePlan.LColumn;
        lSurfaceCount = lMinimumWidths.Count;
        lSurfaceExportIndex = lExportIndex;
    }

    public LColumnPlan LSurfacePlan => lSurfacePlan;

    public int LSurfaceExportIndex => lSurfaceExportIndex;

    public bool LSurfaceExportHidden =>
        lSurfaceExportIndex >= 0 && lSurfaceColumn.LColumnHiddenCheck(lSurfaceExportIndex);

    public bool LSurfaceAutoRelay => lSurfaceAction?.LActionAutoRelay ?? false;

    public bool LSurfaceCollapsedCheck(int lIndex) => lSurfaceColumn.LColumnFixedRead(lIndex) > 0;

    public bool LSurfaceToggleResolve() => !LSurfaceExportHidden;

    public double LSurfaceWidthResolve(double lColumnTotal) => lColumnTotal + LSurfaceWidthPadding;

    public static IReadOnlyList<int> LSurfaceIndexesResolve(int lCount) => Enumerable.Range(0, lCount).ToArray();

    public static IReadOnlyList<int> LSurfaceSplittersResolve(int lCount) =>
        Enumerable.Range(0, Math.Max(0, lCount - 1)).ToArray();

    public static int LSurfaceColumnResolve(int lIndex) => lIndex * 2;

    public static int LSurfaceGutterResolve(int lIndex) => lIndex * 2 + 1;

    public static int LSurfaceLeftResolve(int lExportIndex) => lExportIndex - 1;

    public static double LSurfaceMinimumResolve(double lElementMinimum, double lTableMinimum) =>
        lElementMinimum > 0 ? lElementMinimum : lTableMinimum;

    public static double LSurfaceCollapseResolve(bool lCollapsed, double lStripWidth) => lCollapsed ? lStripWidth : 0;

    public static bool LSurfaceExportResolve(LSceneTabEntry? lLayout) => lLayout?.LSceneExportHidden == true;

    public static bool LSurfaceAutoResolve(LSceneTabEntry? lLayout) => lLayout?.LSceneAutoRelay ?? false;

    public IReadOnlyList<int> LSurfaceCollapsedResolve(LSceneTabEntry? lLayout) =>
        lLayout?.LScenePanelsCollapsed
            .Where(lIndex => lIndex >= 0 && lIndex < lSurfaceCount)
            .ToArray()
        ?? Array.Empty<int>();

    public LSceneTabEntry LSurfaceLayoutRead()
    {
        var lLayout = new LSceneTabEntry
        {
            LSceneExportHidden = LSurfaceExportHidden,
            LSceneAutoRelay = LSurfaceAutoRelay
        };
        for (int lIndex = 0; lIndex < lSurfaceCount; lIndex++)
        {
            if (LSurfaceCollapsedCheck(lIndex))
            {
                lLayout.LScenePanelsCollapsed.Add(lIndex);
            }
        }

        lLayout.LScenePanelWidths.AddRange(lSurfaceColumn.LColumnWeightsRead());
        return lLayout;
    }
}
