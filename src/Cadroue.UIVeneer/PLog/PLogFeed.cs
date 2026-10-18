using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PLogFeed
{
    private readonly ObservableCollection<PLogRow> pLogRowsShown = new();
    private readonly QLog lLog;

    internal PLogFeed(QLog lLogOwner)
    {
        lLog = lLogOwner;
        PLogFeedList = PLogFeedBuild();
    }

    internal ListBox PLogFeedList { get; }

    internal void PLogRowsAttach()
    {
        lLog.QLogFeed.QLogRowsRebuilt += PLogRowsReset;
        lLog.QLogFeed.QLogRowAppended += PLogRowAppend;
        lLog.QLogFeed.QLogRowsRemoved += PLogRowsRemove;
        lLog.QLogScrolled += PLogFeedScroll;
    }

    private void PLogRowsReset()
    {
        pLogRowsShown.Clear();
        lLog.QLogFeed.QLogRowsShown.ToList().ForEach(PLogRowAppend);
    }

    private void PLogRowAppend(QLogRow lRow) => pLogRowsShown.Add(new PLogRow(lRow));

    private void PLogRowsRemove(int pCount) => Enumerable.Range(0, pCount).ToList().ForEach(PLogRowRemove);

    private void PLogRowRemove(int pIndex) => pLogRowsShown.RemoveAt(0);

    private void PLogFeedScroll() => PLogFeedList.ScrollIntoView(pLogRowsShown.LastOrDefault());

    private void PLogScrollHandle(object sender, ScrollChangedEventArgs e)
    {
        ScrollViewer? pViewer = e.OriginalSource as ScrollViewer;
        lLog.QLogScrollHandle(
            ReferenceEquals(pViewer?.TemplatedParent, PLogFeedList),
            e.ExtentHeightChange,
            pViewer?.ScrollableHeight,
            pViewer?.VerticalOffset,
            PLogTemplate.PLogRowHeight);
    }

    private void PLogDetailToggle(object sender, RoutedEventArgs e)
    {
        PLogRow? pRow = QSender.QSenderItemRead<PLogRow>(e.OriginalSource);
        e.Handled = lLog.QLogExpandToggle(pRow?.QLogRow);
        pRow?.PLogRowUpdate();
    }

    private ListBox PLogFeedBuild()
    {
        var pLogFeedList = new ListBox
        {
            ItemsSource = pLogRowsShown,
            BorderThickness = new Thickness(0),
            Background = Brushes.White,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(6)
        };

        PLogTemplate.PLogRowApply(pLogFeedList);
        ScrollViewer.SetHorizontalScrollBarVisibility(pLogFeedList, ScrollBarVisibility.Disabled);
        VirtualizingPanel.SetIsVirtualizing(pLogFeedList, true);
        VirtualizingPanel.SetVirtualizationMode(pLogFeedList, VirtualizationMode.Recycling);
        QScrollbar.QScrollbarAttach(pLogFeedList);
        PLogFeedAttach(pLogFeedList);
        return pLogFeedList;
    }

    private void PLogFeedAttach(ListBox pLogFeedList)
    {
        pLogFeedList.AddHandler(
            ScrollViewer.ScrollChangedEvent,
            new ScrollChangedEventHandler(PLogScrollHandle));
        pLogFeedList.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PLogDetailToggle));
    }
}
