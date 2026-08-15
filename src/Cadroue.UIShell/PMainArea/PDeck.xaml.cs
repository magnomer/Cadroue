using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PControlBar;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public partial class PDeck : UserControl
{
    private PStrip? pStrip;

    public PDeck()
    {
        InitializeComponent();
        Unloaded += PDeckUnloadHandle;
    }

    public void PDeckTabsetSet(PStrip lTabsetValue)
    {
        if (pStrip is not null)
        {
            pStrip.PStripSelectChange -= PDeckSelectHandle;
            pStrip.PStripRecords.CollectionChanged -= PDeckRecordsHandle;
        }

        pStrip = lTabsetValue;
        pStrip.PStripSelectChange += PDeckSelectHandle;
        pStrip.PStripRecords.CollectionChanged += PDeckRecordsHandle;
        PDeckLayoutApply(pStrip.PStripSelected);
    }

    private void PDeckSelectHandle(PTabRecord? pTabRecord)
    {
        PDeckLayoutApply(pTabRecord);
    }

    private void PDeckRecordsHandle(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        PDeckClosedRemove();
    }

    private void PDeckUnloadHandle(object sender, RoutedEventArgs e)
    {
        if (pStrip is not null)
        {
            pStrip.PStripSelectChange -= PDeckSelectHandle;
            pStrip.PStripRecords.CollectionChanged -= PDeckRecordsHandle;
            pStrip = null;
        }
    }

    private void PDeckLayoutApply(PTabRecord? pTabRecord)
    {
        LTraceLog.LTraceInfoRecord(
            $"[SUSPICION] PDeckLayoutApply arg={(pTabRecord is null ? "NULL" : pTabRecord.PTabTitle)}, "
            + $"strip records={(pStrip?.PStripRecords.Count.ToString() ?? "no-strip")}, grid children={pDeckGrid.Children.Count}");

        foreach (UIElement pChild in pDeckGrid.Children)
        {
            pChild.Visibility = Visibility.Collapsed;
        }

        if (pTabRecord is null)
        {
            PDeckNoticeShow();
            return;
        }

        FrameworkElement pTabDeckRoot = pTabRecord.PTabWorkspace.PWorkspaceRoot;
        if (!pDeckGrid.Children.Contains(pTabDeckRoot))
        {
            pDeckGrid.Children.Add(pTabDeckRoot);
        }

        pTabDeckRoot.Visibility = Visibility.Visible;
    }

    private void PDeckNoticeShow()
    {
        const string pDeckNoticeName = "pDeckEmptyNotice";
        TextBlock? pEmptyNotice = pDeckGrid.Children
            .OfType<TextBlock>()
            .FirstOrDefault(pChild => pChild.Name == pDeckNoticeName);

        if (pEmptyNotice is null)
        {
            pEmptyNotice = new TextBlock
            {
                Name = pDeckNoticeName,
                Text = LLocalization.LLocalizationTextRead("Deck.Empty.Notice"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20
            };
            pDeckGrid.Children.Add(pEmptyNotice);
        }

        pEmptyNotice.Visibility = Visibility.Visible;
    }

    private void PDeckClosedRemove()
    {
        if (pStrip is null)
        {
            return;
        }

        var pOpenRoots = pStrip.PStripRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceRoot)
            .ToHashSet();

        for (int index = pDeckGrid.Children.Count - 1; index >= 0; index--)
        {
            if (pDeckGrid.Children[index] is FrameworkElement pGrid && !pOpenRoots.Contains(pGrid))
            {
                LTraceLog.LTraceInfoRecord(
                    $"[SUSPICION] PDeckClosedRemove removes child idx={index}, "
                    + $"strip records={pStrip.PStripRecords.Count}");
                pDeckGrid.Children.RemoveAt(index);
            }
        }
    }
}
