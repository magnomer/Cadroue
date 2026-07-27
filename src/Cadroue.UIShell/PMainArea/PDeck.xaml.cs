using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PControlBar;

namespace Cadroue.UIShell.PMainArea;

public partial class PDeck : UserControl
{
    private LTabset? lTabset;

    public PDeck()
    {
        InitializeComponent();
        Unloaded += PDeckUnloadHandle;
    }

    public void PDeckTabsetSet(LTabset lTabsetValue)
    {
        if (lTabset is not null)
        {
            lTabset.LTabsetSelectChange -= PDeckSelectHandle;
            lTabset.PTabsetRecords.CollectionChanged -= PDeckRecordsHandle;
        }

        lTabset = lTabsetValue;
        lTabset.LTabsetSelectChange += PDeckSelectHandle;
        lTabset.PTabsetRecords.CollectionChanged += PDeckRecordsHandle;
        PDeckLayoutApply(lTabset.PTabsetSelectRecord);
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
        if (lTabset is not null)
        {
            lTabset.LTabsetSelectChange -= PDeckSelectHandle;
            lTabset.PTabsetRecords.CollectionChanged -= PDeckRecordsHandle;
            lTabset = null;
        }
    }

    private void PDeckLayoutApply(PTabRecord? pTabRecord)
    {
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
                Text = "No tab is open.",
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
        if (lTabset is null)
        {
            return;
        }

        var pOpenRoots = lTabset.PTabsetRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PWorkspaceRoot)
            .ToHashSet();

        for (int index = pDeckGrid.Children.Count - 1; index >= 0; index--)
        {
            if (pDeckGrid.Children[index] is FrameworkElement pGrid && !pOpenRoots.Contains(pGrid))
            {
                pDeckGrid.Children.RemoveAt(index);
            }
        }
    }
}
