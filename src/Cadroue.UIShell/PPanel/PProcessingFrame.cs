using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PProcessing
{
    private Button? pProcessingMonitorButton;

    public void PProcessingMonitorSet()
    {
        if (pProcessingMonitorButton is not null)
        {
            pProcessingMonitorButton.Visibility = Visibility.Visible;
        }
    }

    private UIElement PProcessingHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Processing.Header.Title"),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PProcessingButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Processing.Hide.Tooltip"),
            () => PProcessingMinimizeSet(true));
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = pProcessingLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PProcessingActionBuild()
    {
        Button pUpButton = PProcessingButtonBuild(
            PProcessingUpIcon,
            LLocalization.LLocalizationTextRead("Processing.MoveUp.Tooltip"),
            () => PProcessingStepMove(-1));
        pUpButton.Margin = new Thickness(0, 0, 2, 0);
        Button pDownButton = PProcessingButtonBuild(
            PProcessingDownIcon,
            LLocalization.LLocalizationTextRead("Processing.MoveDown.Tooltip"),
            () => PProcessingStepMove(1));

        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(pUpButton);
        pLeftPanel.Children.Add(pDownButton);

        Button pMonitorButton = PProcessingButtonBuild(
            PProcessingMonitorIcon,
            LLocalization.LLocalizationTextRead("NormalizePreview.Button.Tooltip"),
            () => PProcessingMonitorShow?.Invoke());
        pMonitorButton.HorizontalAlignment = HorizontalAlignment.Right;
        pMonitorButton.Visibility = Visibility.Collapsed;
        pProcessingMonitorButton = pMonitorButton;

        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6) };
        pActionGrid.Children.Add(pLeftPanel);
        pActionGrid.Children.Add(pMonitorButton);

        return new Border
        {
            BorderBrush = pProcessingLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionGrid
        };
    }

    private static Button PProcessingButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pProcessingIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }
}
