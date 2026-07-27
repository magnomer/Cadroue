using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PRoster
{
    private const double PRosterProgressHeight = 6;

    private UIElement PRosterTransportBuild()
    {
        var pButtons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        pButtons.Children.Add(pRosterStartButton);
        pButtons.Children.Add(pRosterPauseButton);
        pButtons.Children.Add(pRosterCancelButton);
        pButtons.Children.Add(new Border { Width = 10 });
        pButtons.Children.Add(pRosterRemoveButton);
        pButtons.Children.Add(pRosterClearButton);
        pButtons.Children.Add(pRosterEmptyButton);

        var pRow = new Grid();
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(pButtons, 0);
        Grid.SetColumn(pRosterStatus, 1);
        pRosterStatus.Margin = new Thickness(14, 0, 0, 0);
        pRow.Children.Add(pButtons);
        pRow.Children.Add(pRosterStatus);

        var pStack = new StackPanel();
        pStack.Children.Add(pRow);
        pStack.Children.Add(new Border { Height = 10 });
        pStack.Children.Add(pRosterProgress);

        return new Border
        {
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(12, 10, 12, 12),
            Background = Brushes.White,
            BorderBrush = PRosterTheme.PRosterLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(PRosterTheme.PRosterCorner),
            SnapsToDevicePixels = true,
            Child = pStack
        };
    }

    private static ProgressBar PRosterProgressBuild() => new()
    {
        Height = PRosterProgressHeight,
        Minimum = 0,
        Maximum = 1,
        Value = 0,
        Background = PRosterTheme.PRosterTrackBrush,
        Foreground = PRosterTheme.PRosterRunBrush,
        BorderThickness = new Thickness(0),
        Template = PRosterProgressTemplateCreate()
    };

    private static ControlTemplate PRosterProgressTemplateCreate()
    {
        var pTrack = new FrameworkElementFactory(typeof(Border));
        pTrack.Name = "PART_Track";
        pTrack.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
        pTrack.SetValue(Border.CornerRadiusProperty, new CornerRadius(PRosterProgressHeight / 2));

        var pIndicator = new FrameworkElementFactory(typeof(Border));
        pIndicator.Name = "PART_Indicator";
        pIndicator.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
        pIndicator.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.ForegroundProperty));
        pIndicator.SetValue(Border.CornerRadiusProperty, new CornerRadius(PRosterProgressHeight / 2));

        var pRoot = new FrameworkElementFactory(typeof(Grid));
        pRoot.AppendChild(pTrack);
        pRoot.AppendChild(pIndicator);

        return new ControlTemplate(typeof(ProgressBar)) { VisualTree = pRoot };
    }
}
