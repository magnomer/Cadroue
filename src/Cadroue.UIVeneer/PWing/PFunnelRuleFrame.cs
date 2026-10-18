using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PFunnelRuleFrame
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pFunnelActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private static readonly IReadOnlyDictionary<bool, Brush> pFunnelSelectBrushes = new Dictionary<bool, Brush>
    {
        [true] = pFunnelActiveBrush,
        [false] = Brushes.Transparent,
    };

    private static readonly IReadOnlyDictionary<bool, Thickness> pFunnelHeaderBorders =
        new Dictionary<bool, Thickness>
        {
            [true] = new Thickness(0),
            [false] = new Thickness(0, 0, 0, 1),
        };

    private const double PFunnelBadgeSize = 18;
    private const double PFunnelBadgeRadius = 9;

    private readonly TextBlock pFunnelOrderBadge;
    private readonly Border pFunnelTitleBar;
    private readonly UIElement pFunnelBody;
    private readonly PFunnelRuleFold pFunnelFold;

    public PFunnelRuleFrame(UIElement pBody, string pTitleKey, Action pRemove, Action pFold)
    {
        pFunnelBody = pBody;
        pFunnelOrderBadge = PFunnelBadgeBuild();
        pFunnelFold = new PFunnelRuleFold(pFold);
        pFunnelTitleBar = PFunnelTitleBuild(QLocalizationCatalog.QLocalizationTextRead(pTitleKey), pRemove);
    }

    public Border PFunnelHeader => pFunnelTitleBar;

    public void PFunnelOrderSet(int pOrder) => pFunnelOrderBadge.Text = PFunnelOrderFormat(pOrder);

    public void PFunnelSelectSet(bool pSelected) => pFunnelTitleBar.Background = pFunnelSelectBrushes[pSelected];

    public void PFunnelCollapsedSet(bool pCollapsed)
    {
        PFunnelCollapsedApply(pCollapsed);
        pFunnelFold.PFunnelTipShow(pCollapsed);
    }

    private static TextBlock PFunnelBadgeBuild() => new()
    {
        FontSize = 12,
        FontFamily = pFunnelFontFamily,
        FontWeight = FontWeights.SemiBold,
        Foreground = Brushes.White,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Center
    };

    private static string PFunnelOrderFormat(int pOrder) => pOrder.ToString();

    private void PFunnelCollapsedApply(bool pCollapsed)
    {
        pFunnelBody.Visibility = PLook.PLookVisible[!pCollapsed];
        pFunnelTitleBar.BorderThickness = pFunnelHeaderBorders[pCollapsed];
        pFunnelFold.PFunnelCollapsedApply(pCollapsed);
    }

    private Border PFunnelTitleBuild(string pTitle, Action pRemove)
    {
        var pTitleLabel = new TextBlock
        {
            Text = pTitle,
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pFunnelTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        var pBadge = new Border
        {
            MinWidth = PFunnelBadgeSize,
            Height = PFunnelBadgeSize,
            CornerRadius = new CornerRadius(PFunnelBadgeRadius),
            Background = pFunnelAccentBrush,
            Padding = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Child = pFunnelOrderBadge
        };

        var pBar = new DockPanel { LastChildFill = true };
        Button pRemoveButton = PFunnelRemoveBuild(pRemove);
        DockPanel.SetDock(pRemoveButton, Dock.Right);
        DockPanel.SetDock(pFunnelFold.PFunnelFoldButton, Dock.Right);
        DockPanel.SetDock(pBadge, Dock.Left);
        pBar.Children.Add(pRemoveButton);
        pBar.Children.Add(pFunnelFold.PFunnelFoldButton);
        pBar.Children.Add(pBadge);
        pBar.Children.Add(pTitleLabel);

        return new Border
        {
            Padding = new Thickness(10, 4, 6, 4),
            BorderBrush = pFunnelLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Child = pBar
        };
    }

    private static Button PFunnelRemoveBuild(Action pRemove)
    {
        Button pButton = PFunnelRemoveCreate();
        PFunnelRemoveAttach(pButton, pRemove);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookChrome);
        return pButton;
    }

    private static Button PFunnelRemoveCreate() => new()
    {
        Width = 20,
        Height = 20,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Center,
        Content = "×",
        FontSize = 15,
        Foreground = pFunnelMutedBrush,
        Padding = new Thickness(0),
        Cursor = Cursors.Hand,
        ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Funnel.Remove")
    };

    private static void PFunnelRemoveAttach(Button pButton, Action pRemove) => pButton.Click += (_, _) => pRemove();
}
