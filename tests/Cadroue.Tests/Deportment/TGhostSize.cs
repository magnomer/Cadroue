using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TGhostSize
{
    [Fact]
    public void SizeResolve_ScalesByDpiAndCeils()
    {
        LGhostSize size = TInterface.TGhostSizeResolve(100.2, 40, 1.5, 1.25);

        Assert.Equal(151, size.LGhostPixelWidth);
        Assert.Equal(50, size.LGhostPixelHeight);
        Assert.Equal(144, size.LGhostDpiX);
        Assert.Equal(120, size.LGhostDpiY);
    }

    [Fact]
    public void SizeResolve_NeverFallsBelowOnePixel()
    {
        LGhostSize size = TInterface.TGhostSizeResolve(0, -5, 1, 1);

        Assert.Equal(1, size.LGhostPixelWidth);
        Assert.Equal(1, size.LGhostPixelHeight);
    }

    [Fact]
    public void Point_OffsetsByTheGrab_AndClearReportsOnce()
    {
        LGhost ghost = TInterface.TGhostCreate(10, 4);

        TInterface.TGhostPointSet(ghost, 110, 54);
        Assert.Equal(100, ghost.LGhostLeft);
        Assert.Equal(50, ghost.LGhostTop);
        Assert.True(ghost.LGhostShown);

        Assert.True(TInterface.TGhostClear(ghost));
        Assert.False(TInterface.TGhostClear(ghost));
        Assert.False(ghost.LGhostShown);
    }

    [Fact]
    public void Picker_BuildsItemsAndSummary()
    {
        IReadOnlyList<LPickerItem> items = TInterface.TPickerItemsCreate(["a", "b", "c"], ["c"]);

        Assert.Equal([false, false, true], items.Select(item => item.LPickerItemChecked));
        Assert.Equal(("none", true), TInterface.TPickerSummaryResolve([], "none"));
        Assert.Equal(("A, C", false), TInterface.TPickerSummaryResolve(["A", "C"], "none"));
    }
}
