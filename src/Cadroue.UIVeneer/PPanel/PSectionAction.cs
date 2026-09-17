using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PSection
{
    private UIElement PSectionActionBuild()
    {
        var pActionLeft = new StackPanel { Orientation = Orientation.Horizontal };
        pActionLeft.Children.Add(PSectionButtonBuild(
            "/PAsset/PPanel/PSort.svg",
            LLocalization.LLocalizationTextRead("Section.Sort.Tooltip"),
            PSectionSortHandle));

        Button pSectionRemoveAllButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListRemoveAll.svg",
            LLocalization.LLocalizationTextRead("Section.RemoveAll.Tooltip"),
            PSectionClearHandle);
        pSectionRemoveAllButton.Margin = new Thickness(PSectionActionGap, 0, 0, 0);
        var pActionRight = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pActionRight.Children.Add(PSectionButtonBuild(
            "/PAsset/PPanel/PExportMinus.svg",
            LLocalization.LLocalizationTextRead("Section.Delete.Tooltip"),
            PSectionDeleteHandle));
        pActionRight.Children.Add(pSectionRemoveAllButton);

        var pActionPanel = new Grid { Margin = new Thickness(10, 4, 10, 4) };
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pActionRight, 1);
        pActionPanel.Children.Add(pActionLeft);
        pActionPanel.Children.Add(pActionRight);

        return new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionPanel
        };
    }

    private Button PSectionButtonBuild(string pIconPath, string pTooltip, RoutedEventHandler pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += pClick;
        return pButton;
    }

    private void PSectionDeleteHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSectionEditCommit();
        pFlowAttached?.PFlowSectionDelete();
    }

    private void PSectionSortHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSectionEditCommit();
        pFlowAttached?.PFlowSectionSort();
    }

    private void PSectionClearHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSectionEditCommit();
        pFlowAttached?.PFlowSectionClear();
    }
}
