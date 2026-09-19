using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TRailDrag
{
    private static LStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static LStripTab TStripTabAdd(LStrip strip, string key)
    {
        LStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    [Fact]
    public void Press_SelectsAndArmsDrag_UntilThreshold()
    {
        LStrip strip = TStripBuild();
        LStripTab first = TStripTabAdd(strip, "Split");
        LStripTab second = TStripTabAdd(strip, "Edit");
        LRail rail = TInterface.TRailCreate(strip);

        Assert.True(TInterface.TRailPressHandle(rail, second, 1, false, 100, 20, 5, 5));
        Assert.Same(second, strip.LStripSelected);
        Assert.Same(second, rail.LRailDragTab);
        Assert.False(rail.LRailDragActive);
        Assert.True(TInterface.TRailMoveCheck(rail, true));
        Assert.False(TInterface.TRailMoveCheck(rail, false));

        Assert.False(TInterface.TRailDragResolve(rail, 102, 21, 4, 4));
        Assert.False(rail.LRailDragActive);
        Assert.True(TInterface.TRailDragResolve(rail, 110, 20, 4, 4));
        Assert.True(rail.LRailDragActive);
        Assert.False(TInterface.TRailDragResolve(rail, 120, 20, 4, 4));

        TInterface.TRailDragMove(rail, 10, [50, 150]);
        Assert.Equal([second, first], strip.LStripTabs);

        Assert.Same(second, TInterface.TRailReleaseResolve(rail));
        Assert.Null(rail.LRailDragTab);
        Assert.False(rail.LRailDragActive);
    }

    [Fact]
    public void DoubleClickOnName_StartsEditing_WithoutDrag()
    {
        LStrip strip = TStripBuild();
        LStripTab tab = TStripTabAdd(strip, "Split");
        LRail rail = TInterface.TRailCreate(strip);

        Assert.False(TInterface.TRailPressHandle(rail, tab, 2, true, 0, 0, 0, 0));
        Assert.True(tab.LStripTabEditing);
        Assert.Null(rail.LRailDragTab);
        Assert.Null(TInterface.TRailReleaseResolve(rail));

        Assert.True(TInterface.TRailKeyHandle(rail, tab, true, false, "Renamed"));
        Assert.False(tab.LStripTabEditing);
        Assert.Equal("Renamed", tab.LStripTabTitle);

        TInterface.TStripEditSet(strip, tab, true);
        Assert.True(TInterface.TRailKeyHandle(rail, tab, false, true, "Ignored"));
        Assert.False(tab.LStripTabEditing);
        Assert.Equal("Renamed", tab.LStripTabTitle);
        Assert.False(TInterface.TRailKeyHandle(rail, tab, false, false, "Ignored"));
        Assert.False(TInterface.TRailKeyHandle(rail, null, true, false, "Ignored"));
    }

    [Fact]
    public void OutsideClick_CommitsOnlyWhileEditingAndOutside()
    {
        LStrip strip = TStripBuild();
        LStripTab tab = TStripTabAdd(strip, "Split");
        LRail rail = TInterface.TRailCreate(strip);

        TInterface.TRailOutsideHandle(rail, tab, false, "Skipped");
        Assert.Equal("Split", tab.LStripTabTitle);

        TInterface.TStripEditSet(strip, tab, true);
        TInterface.TRailOutsideHandle(rail, tab, true, "Skipped");
        Assert.True(tab.LStripTabEditing);
        TInterface.TRailOutsideHandle(rail, tab, false, "Outside");
        Assert.False(tab.LStripTabEditing);
        Assert.Equal("Outside", tab.LStripTabTitle);
    }

    [Fact]
    public void IndexResolve_CountsCentersPassed_AndClamps()
    {
        Assert.Equal(0, TInterface.TRailIndexResolve(10, []));
        Assert.Equal(0, TInterface.TRailIndexResolve(10, [50, 150, 250]));
        Assert.Equal(1, TInterface.TRailIndexResolve(60, [50, 150, 250]));
        Assert.Equal(2, TInterface.TRailIndexResolve(200, [50, 150, 250]));
        Assert.Equal(2, TInterface.TRailIndexResolve(900, [50, 150, 250]));
        Assert.Equal(1, TInterface.TRailIndexResolve(60, [50, double.NaN, 250]));
    }

    [Fact]
    public void InsideAndNameHit_UseBounds()
    {
        Assert.True(TInterface.TRailInsideCheck(false, 0, 0, 100, 50));
        Assert.True(TInterface.TRailInsideCheck(false, 100, 50, 100, 50));
        Assert.False(TInterface.TRailInsideCheck(false, -1, 0, 100, 50));
        Assert.False(TInterface.TRailInsideCheck(false, 0, 51, 100, 50));
        Assert.False(TInterface.TRailInsideCheck(true, 10, 10, 100, 50));

        Assert.True(TInterface.TRailNameCheck(true, 5, 5, 40, 20));
        Assert.False(TInterface.TRailNameCheck(false, 5, 5, 40, 20));
        Assert.False(TInterface.TRailNameCheck(true, 41, 5, 40, 20));
    }
}
