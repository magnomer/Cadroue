using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    public LPreferenceTabLayoutRecord PRosterLayoutRead()
    {
        var lPreferenceTabLayout = new LPreferenceTabLayoutRecord();
        if (pRosterBody.Tag is not PResizableColumnLayout pRosterLayout)
        {
            return lPreferenceTabLayout;
        }

        foreach (double pWidth in pRosterLayout.PWidthsRead())
        {
            lPreferenceTabLayout.PanelWidths.Add(pWidth);
        }

        return lPreferenceTabLayout;
    }

    private UIElement PRosterBuild(LPreferenceTabLayoutRecord? lPreferenceTabLayout)
    {
        var pRoot = new DockPanel { Margin = new Thickness(14) };

        var pTransport = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        pTransport.Children.Add(pRosterStartButton);
        pTransport.Children.Add(pRosterPauseButton);
        pTransport.Children.Add(PRosterButtonBuild("Cancel", PRosterCancelHandle));
        pTransport.Children.Add(new Border { Width = 14 });
        pTransport.Children.Add(pRosterStatus);
        DockPanel.SetDock(pTransport, Dock.Top);
        pRoot.Children.Add(pTransport);

        var pProgressBox = new Border { Margin = new Thickness(0, 0, 0, 14), Child = pRosterProgress };
        DockPanel.SetDock(pProgressBox, Dock.Top);
        pRoot.Children.Add(pProgressBox);

        var pLeftColumn = new ColumnDefinition
        {
            Width = new GridLength(2, GridUnitType.Star),
            MinWidth = 240
        };
        pRosterBody.ColumnDefinitions.Add(pLeftColumn);
        pRosterBody.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(6) });
        var pRightColumn = new ColumnDefinition
        {
            Width = new GridLength(1, GridUnitType.Star),
            MinWidth = 200
        };
        pRosterBody.ColumnDefinitions.Add(pRightColumn);

        Grid.SetColumn(pRosterTable, 0);
        pRosterBody.Children.Add(pRosterTable);

        var pRosterLayout = PResizableColumnLayout.PAttach(
            pRosterBody,
            new[] { pLeftColumn, pRightColumn },
            lPreferenceTabLayout?.PanelWidths);
        var pSplitter = pRosterLayout.PSplitterBuild(0);
        Grid.SetColumn(pSplitter, 1);
        pRosterBody.Children.Add(pSplitter);
        pRosterBody.Tag = pRosterLayout;

        UIElement pDetail = PRosterDetailBuild();
        Grid.SetColumn(pDetail, 2);
        pRosterBody.Children.Add(pDetail);

        pRoot.Children.Add(pRosterBody);
        return pRoot;
    }

    private static Button PRosterButtonBuild(string pLabel, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = pLabel,
            Width = 88,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Style = PButton.PButtonWhiteCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private ListView PRosterTableBuild()
    {
        var pView = new GridView();
        pView.Columns.Add(new GridViewColumn
        {
            Header = "Output",
            Width = 240,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkOutputName))
        });
        pView.Columns.Add(new GridViewColumn
        {
            Header = "Start",
            Width = 80,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkStart)) { StringFormat = @"hh\:mm\:ss" }
        });
        pView.Columns.Add(new GridViewColumn
        {
            Header = "Length",
            Width = 80,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkDuration)) { StringFormat = @"hh\:mm\:ss" }
        });
        pView.Columns.Add(new GridViewColumn
        {
            Header = "State",
            Width = 90,
            DisplayMemberBinding = new Binding(nameof(LWorkItem.LWorkStateCurrent)) { Converter = new PRosterStateLabel() }
        });

        var pTable = new ListView
        {
            View = pView,
            ItemsSource = lRosterSchedule.LScheduleRecords,
            BorderBrush = PRosterLineBrush,
            BorderThickness = new Thickness(1),
            IsSynchronizedWithCurrentItem = false
        };
        pTable.SelectionChanged += (_, _) => PRosterDetailUpdate();
        return pTable;
    }
}
