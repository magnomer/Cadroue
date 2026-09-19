using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSurfaceLayout
{
    private static LSurface TSurfaceBuild(int exportIndex = 2) =>
        TInterface.TSurfaceCreate(TInterface.TColumnCreate([100, 100, 100], null, null, -1), 3, exportIndex);

    [Fact]
    public void LayoutRead_ReflectsHiddenExportAndCollapsedColumns()
    {
        LColumn column = TInterface.TColumnCreate([100, 100, 100], null, null, -1);
        LSurface surface = TInterface.TSurfaceCreate(column, 3, 2);

        Assert.False(surface.LSurfaceExportHidden);
        Assert.True(TInterface.TSurfaceToggleResolve(surface));
        TInterface.TColumnHiddenSet(column, 2, true);
        TInterface.TColumnWidthSet(column, 0, 48);

        LSceneTabRecord layout = TInterface.TSurfaceLayoutRead(surface);

        Assert.True(surface.LSurfaceExportHidden);
        Assert.False(TInterface.TSurfaceToggleResolve(surface));
        Assert.True(layout.LSceneExportHidden);
        Assert.False(layout.LSceneAutoRelay);
        Assert.Equal([0], layout.LScenePanelsCollapsed);
        Assert.Equal(3, layout.LScenePanelWidths.Count);
        Assert.True(TInterface.TSurfaceCollapsedCheck(surface, 0));
        Assert.False(TInterface.TSurfaceCollapsedCheck(surface, 1));
    }

    [Fact]
    public void WithoutExport_NeverHidden()
    {
        LSurface surface = TSurfaceBuild(-1);

        Assert.False(surface.LSurfaceExportHidden);
        Assert.Equal(-1, surface.LSurfaceExportIndex);
        Assert.True(TInterface.TSurfaceToggleResolve(surface));
    }

    [Fact]
    public void CollapsedResolve_KeepsOnlyIndexesInRange()
    {
        LSurface surface = TSurfaceBuild();

        Assert.Empty(TInterface.TSurfaceCollapsedResolve(surface, null));
        Assert.Equal(
            [0, 2],
            TInterface.TSurfaceCollapsedResolve(surface, TInterface.TSceneTabCreate(false, false, 0, -1, 2, 3)));
    }

    [Fact]
    public void StaticResolves_AreArithmeticOverTheGrid()
    {
        LSurface surface = TSurfaceBuild();

        Assert.Equal(316, TInterface.TSurfaceWidthResolve(surface, 300));
        Assert.Equal(90, TInterface.TSurfaceMinimumResolve(90, 300));
        Assert.Equal(300, TInterface.TSurfaceMinimumResolve(0, 300));
        Assert.Equal([0, 1], TInterface.TSurfaceSplittersResolve(3));
        Assert.Empty(TInterface.TSurfaceSplittersResolve(0));
        Assert.Equal(3, TInterface.TSurfaceGutterResolve(1));
    }
}
