using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PProcessing
{
    public void PProcessingSkipSet(bool pProcessingSkipApplied) => LProcessing.LProcessingSkipSet(pProcessingSkipApplied);

    private Border PProcessingSkipBuild()
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(PProcessingSkipIcon, pProcessingIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Tag = PProcessingSkipIcon
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Processing.Skip.Label"),
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            Foreground = pProcessingTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "Label"
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 9, 12, 9),
            Background = Brushes.White,
            BorderBrush = pProcessingLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead("Processing.Skip.Tooltip"),
            Child = pRowContent
        };
        pRowBorder.MouseLeftButtonUp += (_, _) => LProcessing.LProcessingStepSelect(LProcessing.LProcessingSkipStep);
        return pRowBorder;
    }
}
