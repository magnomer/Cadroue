using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PSection
{
    public const double PSectionStripWidth = 48;

    private readonly UIElement pSectionFullBody;
    private readonly UIElement pSectionStripBody;

    public bool PSectionMinimizedCheck() => LSection.LSectionMinimized;

    public void PSectionMinimizeSet(bool pSectionMinimizeRequest) =>
        LSection.LSectionMinimizedSet(pSectionMinimizeRequest);

    private void PSectionMinimizeHandle(bool pSectionMinimized)
    {
        pSectionFullBody.Visibility = pSectionMinimized ? Visibility.Collapsed : Visibility.Visible;
        pSectionStripBody.Visibility = pSectionMinimized ? Visibility.Visible : Visibility.Collapsed;
    }

    private UIElement PSectionHeaderBuild()
    {
        Button pMinimizeButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListMinimize.svg",
            LLocalization.LLocalizationTextRead("Section.Panel.HideTooltip"),
            (_, _) => PSectionMinimizeSet(true));
        pMinimizeButton.Margin = new Thickness(0);
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pSectionCountLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7)),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PSectionStripBuild()
    {
        Button pMaximizeButton = PSectionButtonBuild(
            "/PAsset/PPanel/PListMaximize.svg",
            LLocalization.LLocalizationTextRead("Section.Panel.ShowTooltip"),
            (_, _) => PSectionMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }
}
