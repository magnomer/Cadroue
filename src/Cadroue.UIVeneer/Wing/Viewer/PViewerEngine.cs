using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

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

    public static Border PViewerEngineBuild(QViewerEnginePlan lPlan, Action<string> pChoose)
    {
        var pInner = new StackPanel { Orientation = Orientation.Horizontal };
        pInner.Children.Add(PViewerChoiceBuild(lPlan.QViewerChoiceFlyleaf, pChoose));
        pInner.Children.Add(new Border { Width = 1, Background = pViewerEngineLine });
        pInner.Children.Add(PViewerChoiceBuild(lPlan.QViewerChoiceMpv, pChoose));
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

    private static Border PViewerChoiceBuild(QViewerEngineChoice lChoice, Action<string> pChoose)
    {
        Border pChoice = PViewerChoiceCreate(lChoice);
        PViewerChoiceAttach(pChoice, lChoice, pChoose);
        return pChoice;
    }

    private static Border PViewerChoiceCreate(QViewerEngineChoice lChoice)
    {
        var pLabel = new TextBlock
        {
            Text = lChoice.QViewerChoiceText,
            FontSize = 12,
            FontFamily = pViewerEngineFont,
            FontWeight = PLook.PLookWeight[lChoice.QViewerChoiceActive],
            Foreground = pViewerChoiceText[lChoice.QViewerChoiceActive],
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        return new Border
        {
            Background = pViewerChoiceFill[lChoice.QViewerChoiceActive],
            Padding = new Thickness(12, 3, 12, 3),
            Cursor = PLook.PLookHand[lChoice.QViewerChoiceEnabled],
            Opacity = PLook.PLookOpacity[lChoice.QViewerChoiceEnabled],
            ToolTip = lChoice.QViewerChoiceTip,
            Child = pLabel
        };
    }

    private static void PViewerChoiceAttach(Border pChoice, QViewerEngineChoice lChoice, Action<string> pChoose) =>
        pChoice.MouseLeftButtonUp += (_, _) => pChoose(lChoice.QViewerChoiceKey);
}
