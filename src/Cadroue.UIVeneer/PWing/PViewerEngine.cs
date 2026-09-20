using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal static class PViewerEngine
{
    private static readonly Brush pViewerEngineLine = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly FontFamily pViewerEngineFont = new("Segoe UI");

    private static readonly IReadOnlyDictionary<bool, Brush> pViewerChoiceFill = new Dictionary<bool, Brush>
    {
        [true] = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB)),
        [false] = Brushes.Transparent,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> pViewerChoiceText = new Dictionary<bool, Brush>
    {
        [true] = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A)),
        [false] = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E)),
    };

    public static Border PViewerEngineBuild(LViewerEnginePlan lPlan, Action<string> pChoose)
    {
        var pInner = new StackPanel { Orientation = Orientation.Horizontal };
        pInner.Children.Add(PViewerChoiceBuild(lPlan.LViewerChoiceFlyleaf, pChoose));
        pInner.Children.Add(new Border { Width = 1, Background = pViewerEngineLine });
        pInner.Children.Add(PViewerChoiceBuild(lPlan.LViewerChoiceMpv, pChoose));
        return new Border
        {
            BorderBrush = pViewerEngineLine,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = Brushes.White,
            SnapsToDevicePixels = true,
            Child = pInner
        };
    }

    private static Border PViewerChoiceBuild(LViewerEngineChoice lChoice, Action<string> pChoose)
    {
        var pLabel = new TextBlock
        {
            Text = lChoice.LViewerChoiceText,
            FontSize = 12,
            FontFamily = pViewerEngineFont,
            FontWeight = PLook.PLookWeight[lChoice.LViewerChoiceActive],
            Foreground = pViewerChoiceText[lChoice.LViewerChoiceActive],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var pChoice = new Border
        {
            Background = pViewerChoiceFill[lChoice.LViewerChoiceActive],
            Padding = new Thickness(12, 3, 12, 3),
            Cursor = PLook.PLookHand[lChoice.LViewerChoiceEnabled],
            Opacity = PLook.PLookOpacity[lChoice.LViewerChoiceEnabled],
            ToolTip = lChoice.LViewerChoiceTip,
            Child = pLabel
        };
        pChoice.MouseLeftButtonUp += (_, _) => pChoose(lChoice.LViewerChoiceKey);
        return pChoice;
    }
}
