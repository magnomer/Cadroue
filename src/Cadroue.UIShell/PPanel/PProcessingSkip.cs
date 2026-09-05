using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PProcessing
{
    private bool pProcessingSkipActive;

    public void PProcessingSkipSet(bool pProcessingSkipApplied)
    {
        pProcessingSkipActive = pProcessingSkipApplied;
        if (pProcessingSkipRow.Child is StackPanel pProcessingSkipContent)
        {
            PProcessingRowApply(pProcessingSkipContent, pProcessingSkipApplied);
        }

        pProcessingRowPanel.Opacity = pProcessingSkipApplied ? 0.4 : 1;
        pProcessingActionBar.IsEnabled = !pProcessingSkipApplied;
        pProcessingActionBar.Opacity = pProcessingSkipApplied ? 0.4 : 1;
    }

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
        pRowBorder.MouseLeftButtonUp += (_, _) =>
        {
            pProcessingStepCurrent = PProcessingSkipStep;
            PProcessingSelectApply();
            PProcessingStepChange?.Invoke(PProcessingSkipStep);
            PProcessingStepOpen?.Invoke(PProcessingSkipStep);
        };
        return pRowBorder;
    }
}
