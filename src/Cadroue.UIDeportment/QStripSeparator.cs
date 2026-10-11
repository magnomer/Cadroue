using System.Collections.ObjectModel;

namespace Cadroue.UIDeportment;

public sealed class QStripSeparator
{
    private readonly LStrip lStrip;
    private readonly ObservableCollection<LStripTab> lStripTabs;
    private LStripTab? lStripHovered;
    private bool lStripVertical;

    public QStripSeparator(LStrip lOwner, ObservableCollection<LStripTab> lTabs)
    {
        lStrip = lOwner;
        lStripTabs = lTabs;
    }

    public bool LStripVertical => lStripVertical;

    public void LStripVerticalSet(bool lVertical)
    {
        if (lStripVertical == lVertical)
        {
            return;
        }

        lStripVertical = lVertical;
        LStripHoverClear(null);
    }

    public void LStripHoverSet(LStripTab lTab)
    {
        if (ReferenceEquals(lStripHovered, lTab))
        {
            return;
        }

        lStripHovered = lTab;
        LStripSeparatorUpdate();
    }

    public void LStripHoverClear(LStripTab? lTab)
    {
        if (lStripHovered is null || (lTab is not null && !ReferenceEquals(lStripHovered, lTab)))
        {
            return;
        }

        lStripHovered = null;
        LStripSeparatorUpdate();
    }

    public void LStripHoverReset(LStripTab? lTab)
    {
        if (lTab is null || ReferenceEquals(lStripHovered, lTab))
        {
            lStripHovered = null;
        }
    }

    public void LStripSeparatorUpdate()
    {
        int lSelectedIndex = lStrip.LStripSelected is null ? -1 : lStripTabs.IndexOf(lStrip.LStripSelected);
        int lHoveredIndex = lStripHovered is null ? -1 : lStripTabs.IndexOf(lStripHovered);
        for (int lIndex = 0; lIndex < lStripTabs.Count; lIndex++)
        {
            bool lSeparator = lIndex < lStripTabs.Count - 1
                && lIndex != lSelectedIndex
                && lIndex != lSelectedIndex - 1
                && lIndex != lHoveredIndex
                && lIndex != lHoveredIndex - 1;
            if (lStripTabs[lIndex].LStripTabSeparator == lSeparator)
            {
                continue;
            }

            lStripTabs[lIndex].LStripTabSeparator = lSeparator;
            lStrip.LStripTabRaise(lStripTabs[lIndex]);
        }
    }
}
