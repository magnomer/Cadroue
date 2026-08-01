using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

public sealed partial class PLogWindow
{
    private const string PLogOpenIcon = "/PAssets/PPanels/POpen.svg";

    private UIElement PLogContentBuild()
    {
        var pLogRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };

        var pLogBody = new DockPanel
        {
            Background = Brushes.White,
            Margin = new Thickness(0, PSCasement.PSCasementBandHeight, 0, 0)
        };

        DockPanel pLogFileRow = PLogFileBuild();
        DockPanel.SetDock(pLogFileRow, Dock.Top);
        pLogBody.Children.Add(pLogFileRow);

        DockPanel pLogFilterRow = PLogFilterBuild();
        DockPanel.SetDock(pLogFilterRow, Dock.Top);
        pLogBody.Children.Add(pLogFilterRow);

        pLogBody.Children.Add(new Border
        {
            BorderBrush = PSField.PSFieldLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(18, 0, 18, 18),
            Child = pLogFeed
        });

        pLogRoot.Children.Add(PSCasement.PSCasementBandBuild());
        pLogRoot.Children.Add(pLogBody);
        pLogRoot.Children.Add(PSCasement.PSCasementOverlayBuild(
            this,
            0,
            LLocalization.LLocalizationTextRead("Log.Window.Title")));
        return pLogRoot;
    }

    private DockPanel PLogFileBuild()
    {
        var pLogFileRow = new DockPanel { Margin = new Thickness(18, 14, 18, 0), LastChildFill = false };

        Button pLogOpenButton = PSField.PSInlineIconBuild(
            PLogOpenIcon,
            LLocalization.LLocalizationTextRead("Log.Button.Open"),
            new Thickness(0, 0, 8, 0));
        pLogOpenButton.Click += (_, _) => PLogFolderOpen();
        Button pLogCopyButton = PLogButtonBuild(
            LLocalization.LLocalizationTextRead("Log.Button.Copy"),
            (_, _) => PLogTextCopy());

        DockPanel.SetDock(pLogFileCombo, Dock.Left);
        DockPanel.SetDock(pLogOpenButton, Dock.Left);
        DockPanel.SetDock(pLogCopyButton, Dock.Right);
        pLogFileRow.Children.Add(pLogFileCombo);
        pLogFileRow.Children.Add(pLogOpenButton);
        pLogFileRow.Children.Add(pLogCopyButton);
        return pLogFileRow;
    }

    private DockPanel PLogFilterBuild()
    {
        var pLogFilterRow = new DockPanel { Margin = new Thickness(18, 9, 18, 12), LastChildFill = false };
        DockPanel.SetDock(pLogCategoryCombo, Dock.Left);
        DockPanel.SetDock(pLogVerboseBox, Dock.Right);
        pLogFilterRow.Children.Add(pLogCategoryCombo);
        pLogFilterRow.Children.Add(pLogVerboseBox);
        return pLogFilterRow;
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

        PLogRow.PLogRowApply(pLogFeedList);
        ScrollViewer.SetHorizontalScrollBarVisibility(pLogFeedList, ScrollBarVisibility.Disabled);
        VirtualizingPanel.SetIsVirtualizing(pLogFeedList, true);
        VirtualizingPanel.SetVirtualizationMode(pLogFeedList, VirtualizationMode.Recycling);
        PScrollbar.PScrollbarApply(pLogFeedList);
        pLogFeedList.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PLogDetailToggle));
        return pLogFeedList;
    }

    private CheckBox PLogVerboseBuild()
    {
        var pLogVerbose = new CheckBox
        {
            Content = LLocalization.LLocalizationTextRead("Log.Verbose.Label"),
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = LTrace.LTraceVerbose,
            ToolTip = LLocalization.LLocalizationTextRead("Log.Verbose.Tooltip")
        };

        PCheckbox.PCheckboxApply(pLogVerbose);
        pLogVerbose.Checked += (_, _) => PLogVerboseSet(true);
        pLogVerbose.Unchecked += (_, _) => PLogVerboseSet(false);
        return pLogVerbose;
    }

    private static ComboBox PLogComboBuild(double pLogWidth)
    {
        var pLogCombo = new ComboBox
        {
            Width = pLogWidth,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        PDropdown.PDropdownApply(pLogCombo);
        return pLogCombo;
    }

    private static Button PLogButtonBuild(string pLogText, RoutedEventHandler pLogClick)
    {
        var pLogButton = new Button
        {
            Content = pLogText,
            MinWidth = 84,
            Height = PSField.PSFieldControlHeight,
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Style = PButton.PButtonWhiteCreate()
        };
        pLogButton.Click += pLogClick;
        return pLogButton;
    }
}
