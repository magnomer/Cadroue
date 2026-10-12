using System.Collections.ObjectModel;

namespace Cadroue.UIDeportment;

public sealed class QStripSeparator
{
    private readonly QStrip qStrip;
    private readonly ObservableCollection<QStripTab> qStripTabs;
    private QStripTab? qStripHovered;
    private bool qStripVertical;

    public QStripSeparator(QStrip lOwner, ObservableCollection<QStripTab> lTabs)
    {
        qStrip = lOwner;
        qStripTabs = lTabs;
    }

    public bool QStripVertical => qStripVertical;

    public void QStripVerticalSet(bool lVertical)
    {
        if (qStripVertical == lVertical)
        {
            return;
        }

        qStripVertical = lVertical;
        QStripHoverClear(null);
    }

    public void QStripHoverSet(QStripTab lTab)
    {
        if (ReferenceEquals(qStripHovered, lTab))
        {
            return;
        }

        qStripHovered = lTab;
        QStripSeparatorUpdate();
    }

    public void QStripHoverClear(QStripTab? lTab)
    {
        if (qStripHovered is null || (lTab is not null && !ReferenceEquals(qStripHovered, lTab)))
        {
            return;
        }

        qStripHovered = null;
        QStripSeparatorUpdate();
    }

    public void QStripHoverReset(QStripTab? lTab)
    {
        if (lTab is null || ReferenceEquals(qStripHovered, lTab))
        {
            qStripHovered = null;
        }
    }

    public void QStripSeparatorUpdate()
    {
        int lSelectedIndex = qStrip.QStripSelected is null ? -1 : qStripTabs.IndexOf(qStrip.QStripSelected);
        int lHoveredIndex = qStripHovered is null ? -1 : qStripTabs.IndexOf(qStripHovered);
        for (int lIndex = 0; lIndex < qStripTabs.Count; lIndex++)
        {
            bool lSeparator = lIndex < qStripTabs.Count - 1
                && lIndex != lSelectedIndex
                && lIndex != lSelectedIndex - 1
                && lIndex != lHoveredIndex
                && lIndex != lHoveredIndex - 1;
            if (qStripTabs[lIndex].QStripTabSeparator == lSeparator)
            {
                continue;
            }

            qStripTabs[lIndex].QStripTabSeparator = lSeparator;
            qStrip.QStripTabRaise(qStripTabs[lIndex]);
        }
    }
}
