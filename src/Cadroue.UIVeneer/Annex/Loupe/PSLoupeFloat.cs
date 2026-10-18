using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSLoupeFloat
{
    private static readonly SolidColorBrush PSLoupeFloatFill = new(Color.FromRgb(0xD3, 0xE1, 0xF2));
    private static readonly SolidColorBrush PSLoupeFloatBorder = new(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private static readonly IReadOnlyDictionary<bool, Brush> psLoupeSegmentFills = new Dictionary<bool, Brush>
    {
        [true] = PSLoupeFloatFill,
        [false] = Brushes.Transparent,
    };

    private readonly QSLoupeFloatChoice lsLoupeFloat;
    private readonly IReadOnlyDictionary<QSLoupeFloat, Button> psLoupeSegments;

    internal PSLoupeFloat(QSLoupeFloatChoice lFloat)
    {
        lsLoupeFloat = lFloat;
        psLoupeSegments = lsLoupeFloat.QSLoupeButtonsRead().ToDictionary(
            lButton => lButton.QSLoupeButtonKind, PSLoupeSegmentBuild);
    }

    internal UIElement PSLoupeStripBuild()
    {
        var pStrip = new StackPanel { Orientation = Orientation.Horizontal };
        psLoupeSegments.Values.ToList().ForEach(pSegment => pStrip.Children.Add(pSegment));
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Child = new Border
            {
                BorderBrush = PSLoupeFloatBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Child = pStrip
            }
        };
    }

    internal void PSLoupeSegmentsApply() => lsLoupeFloat.QSLoupeButtonsRead().ToList().ForEach(PSLoupeSegmentApply);

    private Button PSLoupeSegmentBuild(QSLoupeButton lButton)
    {
        Button pButton = PSLoupeSegmentCreate(lButton);
        PSLoupeSegmentAttach(pButton, lButton);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PSLoupeSegmentCreate(QSLoupeButton lButton) => new()
    {
        Content = lButton.QSLoupeButtonText,
        Height = 24,
        MinWidth = 52,
        Padding = new Thickness(10, 0, 10, 0),
        FontSize = 11
    };

    private void PSLoupeSegmentAttach(Button pButton, QSLoupeButton lButton) =>
        pButton.Click += (_, _) => lsLoupeFloat.QSLoupeFloatSet(lButton.QSLoupeButtonKind);

    private void PSLoupeSegmentApply(QSLoupeButton lButton) =>
        psLoupeSegments[lButton.QSLoupeButtonKind].Background = psLoupeSegmentFills[lButton.QSLoupeButtonSelected];
}
