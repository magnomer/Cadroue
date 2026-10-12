using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TGhostSize
{
    [Fact]
    public void SizeResolve_ScalesByDpiAndCeils()
    {
        QGhostSize size = TInterface.TGhostSizeResolve(100.2, 40, 1.5, 1.25);

        Assert.Equal(151, size.QGhostPixelWidth);
        Assert.Equal(50, size.QGhostPixelHeight);
        Assert.Equal(144, size.QGhostDpiX);
        Assert.Equal(120, size.QGhostDpiY);
    }

    [Fact]
    public void SizeResolve_NeverFallsBelowOnePixel()
    {
        QGhostSize size = TInterface.TGhostSizeResolve(0, -5, 1, 1);

        Assert.Equal(1, size.QGhostPixelWidth);
        Assert.Equal(1, size.QGhostPixelHeight);
    }

    [Fact]
    public void Point_OffsetsByTheGrab_AndClearReportsOnce()
    {
        QGhost ghost = TInterface.TGhostCreate(10, 4);

        TInterface.TGhostPointSet(ghost, 110, 54);
        Assert.Equal(100, ghost.QGhostLeft);
        Assert.Equal(50, ghost.QGhostTop);
        Assert.True(ghost.QGhostShown);

        Assert.True(TInterface.TGhostClear(ghost));
        Assert.False(TInterface.TGhostClear(ghost));
        Assert.False(ghost.QGhostShown);
    }

    [Fact]
    public void Picker_BuildsItemsAndSummary()
    {
        IReadOnlyList<QPickerItem> items = TInterface.TPickerItemsCreate(["a", "b", "c"], ["c"]);

        Assert.Equal([false, false, true], items.Select(item => item.QPickerItemChecked));
        Assert.Equal(("none", true), TInterface.TPickerSummaryResolve([], "none"));
        Assert.Equal(("A, C", false), TInterface.TPickerSummaryResolve(["A", "C"], "none"));
    }
}
