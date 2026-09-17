using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PProcessing
{
    public IReadOnlyList<string> PProcessingStepsRead() => LProcessing.LProcessingSteps;

    public void PProcessingActiveSet(string pStepName, bool pActive) =>
        LProcessing.LProcessingActiveSet(pStepName, pActive);

    public void PProcessingEnabledSet(string pStepName, bool pEnabled, string? pDisabledTooltip = null)
    {
        if (pProcessingRows.TryGetValue(pStepName, out Border? pRowBorder))
        {
            pRowBorder.ToolTip = pEnabled ? null : pDisabledTooltip;
            AutomationProperties.SetHelpText(pRowBorder, pEnabled ? string.Empty : pDisabledTooltip ?? string.Empty);
            ToolTipService.SetShowOnDisabled(pRowBorder, true);
        }

        LProcessing.LProcessingEnabledSet(pStepName, pEnabled);
    }

    private static void PProcessingRowApply(StackPanel pRowContent, bool pActive)
    {
        Brush pTextBrush = pActive ? pProcessingActiveBrush : pProcessingTextBrush;
        Brush pIconBrush = pActive ? pProcessingActiveBrush : pProcessingIconBrush;
        foreach (UIElement pPiece in pRowContent.Children)
        {
            switch (pPiece)
            {
                case Image { Tag: string pIconPath } pIcon:
                    pIcon.Source = PIcon.PIconRead(pIconPath, pIconBrush);
                    break;
                case TextBlock { Tag: "Label" } pText:
                    pText.Foreground = pTextBrush;
                    pText.FontWeight = pActive ? FontWeights.SemiBold : FontWeights.Normal;
                    break;
            }
        }
    }

    public void PProcessingStepAdd(string pStepName, string pStepIconPath, string pStepLabelKey)
    {
        Border pRow = PProcessingRowBuild(pStepName, pStepIconPath, pStepLabelKey);
        pProcessingRows[pStepName] = pRow;
        pProcessingRowPanel.Children.Add(pRow);
        LProcessing.LProcessingStepAdd(pStepName);
        PProcessingNumbersUpdate();
    }

    private static Border PProcessingBadgeBuild()
    {
        var pNumber = new TextBlock
        {
            FontSize = 10,
            FontFamily = pProcessingFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        return new Border
        {
            Width = 18,
            Height = 18,
            CornerRadius = new CornerRadius(9),
            Background = new SolidColorBrush(Color.FromRgb(0xE8, 0xEE, 0xF6)),
            Margin = new Thickness(0, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Child = pNumber
        };
    }

    private Border PProcessingRowBuild(string pStepName, string pStepIconPath, string pStepLabelKey)
    {
        string pStepLabel = LLocalization.LLocalizationTextRead(pStepLabelKey);
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        if (LProcessing.LProcessingOrdered)
        {
            pRowContent.Children.Add(PProcessingBadgeBuild());
        }

        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead(pStepIconPath, pProcessingIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Tag = pStepIconPath
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = pStepLabel,
            FontSize = 12,
            FontFamily = pProcessingFontFamily,
            Foreground = pProcessingTextBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Tag = "Label"
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = Brushes.White,
            BorderBrush = pProcessingLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            Focusable = true,
            Child = pRowContent,
            Tag = pStepName
        };
        AutomationProperties.SetName(pRowBorder, pStepLabel);
        PProcessingRowApply(pRowContent, LProcessing.LProcessingActiveCheck(pStepName));
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            LProcessing.LProcessingStepSelect(pStepName);

            pProcessingRowDragging = pRowBorder;
            LProcessing.LProcessingDragSet(pProcessingRowPanel.Children.IndexOf(pRowBorder));
            pProcessingDragOrigin = pRowEvent.GetPosition(pProcessingRowPanel);
            pProcessingDragActive = false;

            pRowEvent.Handled = true;
        };
        pRowBorder.KeyDown += (_, pRowEvent) =>
        {
            if (pRowEvent.Key is not (Key.Enter or Key.Space))
            {
                return;
            }

            LProcessing.LProcessingStepSelect(pStepName);
            pRowEvent.Handled = true;
        };
        return pRowBorder;
    }
}
