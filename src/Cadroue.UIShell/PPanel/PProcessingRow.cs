using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAsset;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PProcessing
{
    private readonly HashSet<string> pProcessingActiveSteps = new(StringComparer.Ordinal);
    private readonly HashSet<string> pProcessingDisabledSteps = new(StringComparer.Ordinal);

    public IReadOnlyList<string> PProcessingStepsRead()
    {
        var pStepNames = new List<string>();
        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is Border { Tag: string pRowName })
            {
                pStepNames.Add(pRowName);
            }
        }

        return pStepNames;
    }

    public void PProcessingActiveSet(string pStepName, bool pActive)
    {
        if (pActive)
        {
            pProcessingActiveSteps.Add(pStepName);
        }
        else
        {
            pProcessingActiveSteps.Remove(pStepName);
        }

        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is not Border { Tag: string pRowName, Child: StackPanel pRowContent } || pRowName != pStepName)
            {
                continue;
            }

            PProcessingRowApply(pRowContent, pActive);

            return;
        }
    }

    public void PProcessingEnabledSet(string pStepName, bool pEnabled, string? pDisabledTooltip = null)
    {
        if (pEnabled)
        {
            pProcessingDisabledSteps.Remove(pStepName);
        }
        else
        {
            pProcessingDisabledSteps.Add(pStepName);
        }

        foreach (UIElement pRow in pProcessingRowPanel.Children)
        {
            if (pRow is not Border { Tag: string pRowName } pRowBorder || pRowName != pStepName)
            {
                continue;
            }

            pRowBorder.IsEnabled = pEnabled;
            pRowBorder.Opacity = pEnabled ? 1 : 0.4;
            pRowBorder.Cursor = pEnabled ? Cursors.Hand : Cursors.Arrow;
            pRowBorder.ToolTip = pEnabled ? null : pDisabledTooltip;
            AutomationProperties.SetHelpText(pRowBorder, pEnabled ? string.Empty : pDisabledTooltip ?? string.Empty);
            ToolTipService.SetShowOnDisabled(pRowBorder, true);
            if (!pEnabled && ReferenceEquals(pProcessingRowDragging, pRowBorder))
            {
                Mouse.Capture(null);
                PProcessingDragClear();
            }

            PProcessingSelectApply();
            return;
        }
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
        pProcessingRowPanel.Children.Add(PProcessingRowBuild(pStepName, pStepIconPath, pStepLabelKey));
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
        if (pProcessingOrdered)
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
        PProcessingRowApply(pRowContent, pProcessingActiveSteps.Contains(pStepName));
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            PProcessingStepSelect(pStepName);

            pProcessingRowDragging = pRowBorder;
            pProcessingIndexDragging = pProcessingRowPanel.Children.IndexOf(pRowBorder);
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

            PProcessingStepSelect(pStepName);
            pRowEvent.Handled = true;
        };
        return pRowBorder;
    }
}
