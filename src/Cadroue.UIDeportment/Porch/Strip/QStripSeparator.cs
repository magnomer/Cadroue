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

        QStripAxisSet(lVertical);
        QStripHoverClear(null);
    }

    public void QStripHoverSet(QStripTab lTab)
    {
        if (ReferenceEquals(qStripHovered, lTab))
        {
            return;
        }

        QStripHoveredSet(lTab);
        QStripSeparatorUpdate();
    }

    public void QStripHoverClear(QStripTab? lTab)
    {
        if (qStripHovered is null || (lTab is not null && !ReferenceEquals(qStripHovered, lTab)))
        {
            return;
        }

        QStripHoveredSet(null);
        QStripSeparatorUpdate();
    }

    public void QStripHoverReset(QStripTab? lTab)
    {
        if (lTab is null || ReferenceEquals(qStripHovered, lTab))
        {
            QStripHoveredSet(null);
        }
    }

    public void QStripSeparatorUpdate()
    {
        int lSelectedIndex = QStripIndexRead(qStrip.QStripSelected);
        int lHoveredIndex = QStripIndexRead(qStripHovered);
        for (int lIndex = 0; lIndex < qStripTabs.Count; lIndex++)
        {
            QStripSeparatorSet(qStripTabs[lIndex], QStripSeparatorCheck(lIndex, lSelectedIndex, lHoveredIndex));
        }
    }

    private void QStripAxisSet(bool lVertical) => qStripVertical = lVertical;

    private void QStripHoveredSet(QStripTab? lTab) => qStripHovered = lTab;

    private int QStripIndexRead(QStripTab? lTab) => lTab is null ? -1 : qStripTabs.IndexOf(lTab);

    private bool QStripSeparatorCheck(int lIndex, int lSelectedIndex, int lHoveredIndex) =>
        lIndex < qStripTabs.Count - 1
        && lIndex != lSelectedIndex
        && lIndex != lSelectedIndex - 1
        && lIndex != lHoveredIndex
        && lIndex != lHoveredIndex - 1;

    private void QStripSeparatorSet(QStripTab lTab, bool lSeparator)
    {
        if (lTab.QStripTabSeparator == lSeparator)
        {
            return;
        }

        lTab.QStripTabSeparator = lSeparator;
        qStrip.QStripTabRaise(lTab);
    }
}
