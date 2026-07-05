using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PControlBar;

namespace Cadroue.UIShell.PMainArea;

public partial class PMainArea : UserControl
{
    private LTabSelect? lTabSelect;
    private PTabRecord? pMainAreaTabRecord;

    public PMainArea()
    {
        InitializeComponent();
        Unloaded += PMainAreaUnloadedHandle;
    }

    public void PMainAreaTabSelectSet(LTabSelect lTabSelectValue)
    {
        if (lTabSelect is not null)
        {
            lTabSelect.LTabSelectChange -= PMainAreaTabSelectChangeHandle;
            lTabSelect.PTabRecords.CollectionChanged -= PMainAreaTabRecordsChangeHandle;
        }

        lTabSelect = lTabSelectValue;
        lTabSelect.LTabSelectChange += PMainAreaTabSelectChangeHandle;
        lTabSelect.PTabRecords.CollectionChanged += PMainAreaTabRecordsChangeHandle;
        PMainAreaLayoutApply(lTabSelect.PTabSelectRecord);
    }

    private void PMainAreaTabSelectChangeHandle(PTabRecord? pTabRecord)
    {
        PMainAreaLayoutApply(pTabRecord);
    }

    private void PMainAreaTabRecordsChangeHandle(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        PMainAreaClosedTabsRemove();
    }

    private void PMainAreaUnloadedHandle(object sender, RoutedEventArgs e)
    {
        if (lTabSelect is not null)
        {
            lTabSelect.LTabSelectChange -= PMainAreaTabSelectChangeHandle;
            lTabSelect.PTabRecords.CollectionChanged -= PMainAreaTabRecordsChangeHandle;
            lTabSelect = null;
        }

        pMainAreaTabRecord = null;
    }

    private void PMainAreaLayoutApply(PTabRecord? pTabRecord)
    {
        pMainAreaTabRecord = pTabRecord;

        foreach (UIElement pChild in pMainAreaGrid.Children)
        {
            pChild.Visibility = Visibility.Collapsed;
        }

        if (pTabRecord is null)
        {
            PMainAreaEmptyNoticeShow();
            return;
        }

        FrameworkElement pTabMainAreaRoot = pTabRecord.PTabWorkspace.PTabWorkspaceMainAreaRoot;
        if (!pMainAreaGrid.Children.Contains(pTabMainAreaRoot))
        {
            pMainAreaGrid.Children.Add(pTabMainAreaRoot);
        }

        pTabMainAreaRoot.Visibility = Visibility.Visible;
    }

    private void PMainAreaEmptyNoticeShow()
    {
        const string pEmptyNoticeName = "pMainAreaEmptyNotice";
        TextBlock? pEmptyNotice = pMainAreaGrid.Children
            .OfType<TextBlock>()
            .FirstOrDefault(pChild => pChild.Name == pEmptyNoticeName);

        if (pEmptyNotice is null)
        {
            pEmptyNotice = new TextBlock
            {
                Name = pEmptyNoticeName,
                Text = "No tab is open.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 20
            };
            pMainAreaGrid.Children.Add(pEmptyNotice);
        }

        pEmptyNotice.Visibility = Visibility.Visible;
    }

    private void PMainAreaClosedTabsRemove()
    {
        if (lTabSelect is null)
        {
            return;
        }

        var pOpenRoots = lTabSelect.PTabRecords
            .Select(pTabRecord => pTabRecord.PTabWorkspace.PTabWorkspaceMainAreaRoot)
            .ToHashSet();

        for (int index = pMainAreaGrid.Children.Count - 1; index >= 0; index--)
        {
            if (pMainAreaGrid.Children[index] is FrameworkElement pGrid && !pOpenRoots.Contains(pGrid))
            {
                pMainAreaGrid.Children.RemoveAt(index);
            }
        }
    }
}
