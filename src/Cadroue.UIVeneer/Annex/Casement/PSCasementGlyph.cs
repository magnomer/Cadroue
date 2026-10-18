using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal static class PSCasementGlyph
{
    internal static Canvas PSCasementMinimizeBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2, 12, 14, 12));
        return pCanvas;
    }

    internal static Canvas PSCasementMaximizeBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        var pSquare = new System.Windows.Shapes.Rectangle
        {
            Width = 12,
            Height = 12,
            StrokeThickness = 1.2,
            Fill = Brushes.Transparent,
            Margin = new Thickness(2, 2, 0, 0)
        };
        PSCasementGlyphAttach(pSquare);
        pCanvas.Children.Add(pSquare);
        return pCanvas;
    }

    internal static Canvas PSCasementCloseBuild()
    {
        var pCanvas = new Canvas { Width = 18, Height = 16 };
        pCanvas.Children.Add(PSRuleBuild(2.5, 2.5, 13.5, 13.5));
        pCanvas.Children.Add(PSRuleBuild(13.5, 2.5, 2.5, 13.5));
        return pCanvas;
    }

    private static System.Windows.Shapes.Line PSRuleBuild(double pX1, double pY1, double pX2, double pY2)
    {
        var pRule = new System.Windows.Shapes.Line
        {
            X1 = pX1,
            Y1 = pY1,
            X2 = pX2,
            Y2 = pY2,
            StrokeThickness = 1.25,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };
        PSCasementGlyphAttach(pRule);
        return pRule;
    }

    private static void PSCasementGlyphAttach(System.Windows.Shapes.Shape pGlyph)
    {
        pGlyph.SetBinding(System.Windows.Shapes.Shape.StrokeProperty, new System.Windows.Data.Binding
        {
            Path = new PropertyPath(TextElement.ForegroundProperty),
            RelativeSource = new System.Windows.Data.RelativeSource(
                System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(ContentPresenter), 1)
        });
    }
}
