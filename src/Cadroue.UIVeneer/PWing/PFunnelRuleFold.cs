using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelRuleFold
{
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private static readonly IReadOnlyDictionary<bool, string> pFunnelFoldTooltips = new Dictionary<bool, string>
    {
        [true] = "Inspector.Funnel.Maximize",
        [false] = "Inspector.Funnel.Minimize",
    };

    private readonly Action pFunnelFold;
    private readonly IReadOnlyDictionary<bool, UIElement> pFunnelGlyphs = new Dictionary<bool, UIElement>
    {
        [true] = PFunnelSquareBuild(),
        [false] = PFunnelDashBuild(),
    };

    public PFunnelRuleFold(Action pFold)
    {
        pFunnelFold = pFold;
        PFunnelFoldButton = PFunnelFoldBuild();
    }

    public Button PFunnelFoldButton { get; }

    public void PFunnelCollapsedApply(bool pCollapsed) => PFunnelFoldButton.Content = pFunnelGlyphs[pCollapsed];

    public void PFunnelTipShow(bool pCollapsed) =>
        PFunnelFoldButton.ToolTip = QLocalizationCatalog.QLocalizationTextRead(pFunnelFoldTooltips[pCollapsed]);

    private Button PFunnelFoldBuild()
    {
        Button pButton = PFunnelFoldCreate();
        PFunnelFoldAttach(pButton);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookChrome);
        return pButton;
    }

    private Button PFunnelFoldCreate() => new()
    {
        Width = 20,
        Height = 20,
        Padding = new Thickness(0),
        Cursor = Cursors.Hand,
        Content = pFunnelGlyphs[false],
        ToolTip = QLocalizationCatalog.QLocalizationTextRead(pFunnelFoldTooltips[false])
    };

    private void PFunnelFoldAttach(Button pButton) => pButton.Click += (_, _) => pFunnelFold();

    private static Canvas PFunnelGlyphBuild() => new()
    {
        Width = 12,
        Height = 12,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static UIElement PFunnelSquareBuild()
    {
        Canvas pCanvas = PFunnelGlyphBuild();
        var pRect = new System.Windows.Shapes.Rectangle
        {
            Width = 9,
            Height = 9,
            Stroke = pFunnelMutedBrush,
            StrokeThickness = 1.2,
            Fill = Brushes.Transparent
        };
        Canvas.SetLeft(pRect, 1.5);
        Canvas.SetTop(pRect, 1.5);
        pCanvas.Children.Add(pRect);
        return pCanvas;
    }

    private static UIElement PFunnelDashBuild()
    {
        Canvas pCanvas = PFunnelGlyphBuild();
        pCanvas.Children.Add(new System.Windows.Shapes.Line
        {
            X1 = 1.5,
            Y1 = 9,
            X2 = 10.5,
            Y2 = 9,
            Stroke = pFunnelMutedBrush,
            StrokeThickness = 1.25,
            StrokeStartLineCap = PenLineCap.Square,
            StrokeEndLineCap = PenLineCap.Square
        });
        return pCanvas;
    }
}
