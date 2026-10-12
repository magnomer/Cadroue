using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TRailDrag
{
    private static QStrip TStripBuild() =>
        TInterface.TStripCreate(key => key, (name, ordinal) => $"{name} {ordinal}");

    private static QStripTab TStripTabAdd(QStrip strip, string key)
    {
        QStripTab tab = TInterface.TStripTabCreate(key);
        TInterface.TStripAdd(strip, tab);
        return tab;
    }

    [Fact]
    public void Press_SelectsAndArmsDrag_UntilThreshold()
    {
        QStrip strip = TStripBuild();
        QStripTab first = TStripTabAdd(strip, "Split");
        QStripTab second = TStripTabAdd(strip, "Edit");
        QRail rail = TInterface.TRailRead(strip);

        Assert.True(TInterface.TRailPressHandle(rail, second, 1, false, 100, 20, 5, 5));
        Assert.Same(second, strip.QStripSelected);
        Assert.Same(second, rail.QRailDragTab);
        Assert.False(rail.QRailDragActive);
        Assert.True(TInterface.TRailMoveCheck(rail, true));
        Assert.False(TInterface.TRailMoveCheck(rail, false));

        Assert.False(TInterface.TRailDragResolve(rail, 102, 21, 4, 4));
        Assert.False(rail.QRailDragActive);
        Assert.True(TInterface.TRailDragResolve(rail, 110, 20, 4, 4));
        Assert.True(rail.QRailDragActive);
        Assert.False(TInterface.TRailDragResolve(rail, 120, 20, 4, 4));

        TInterface.TRailDragMove(rail, 10, [50, 150]);
        Assert.Equal([second, first], strip.QStripTabs);

        Assert.Same(second, TInterface.TRailReleaseResolve(rail));
        Assert.Null(rail.QRailDragTab);
        Assert.False(rail.QRailDragActive);
    }

    [Fact]
    public void DoubleClickOnName_StartsEditing_WithoutDrag()
    {
        QStrip strip = TStripBuild();
        QStripTab tab = TStripTabAdd(strip, "Split");
        QRail rail = TInterface.TRailRead(strip);

        Assert.False(TInterface.TRailPressHandle(rail, tab, 2, true, 0, 0, 0, 0));
        Assert.True(tab.QStripTabEditing);
        Assert.Null(rail.QRailDragTab);
        Assert.Null(TInterface.TRailReleaseResolve(rail));

        Assert.True(TInterface.TRailKeyHandle(rail, tab, true, false, "Renamed"));
        Assert.False(tab.QStripTabEditing);
        Assert.Equal("Renamed", tab.QStripTabTitle);

        TInterface.TStripEditSet(strip, tab, true);
        Assert.True(TInterface.TRailKeyHandle(rail, tab, false, true, "Ignored"));
        Assert.False(tab.QStripTabEditing);
        Assert.Equal("Renamed", tab.QStripTabTitle);
        Assert.False(TInterface.TRailKeyHandle(rail, tab, false, false, "Ignored"));
        Assert.False(TInterface.TRailKeyHandle(rail, null, true, false, "Ignored"));
    }

    [Fact]
    public void OutsideClick_CommitsOnlyWhileEditingAndOutside()
    {
        QStrip strip = TStripBuild();
        QStripTab tab = TStripTabAdd(strip, "Split");
        QRail rail = TInterface.TRailRead(strip);

        TInterface.TRailOutsideHandle(rail, tab, false, "Skipped");
        Assert.Equal("Split", tab.QStripTabTitle);

        TInterface.TStripEditSet(strip, tab, true);
        TInterface.TRailOutsideHandle(rail, tab, true, "Skipped");
        Assert.True(tab.QStripTabEditing);
        TInterface.TRailOutsideHandle(rail, tab, false, "Outside");
        Assert.False(tab.QStripTabEditing);
        Assert.Equal("Outside", tab.QStripTabTitle);
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
