using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

internal sealed class PFunnelRuleFrame
{
    private static readonly FontFamily pFunnelFontFamily = new("Segoe UI");
    private static readonly Brush pFunnelLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pFunnelTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pFunnelMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pFunnelAccentBrush = new SolidColorBrush(Color.FromRgb(0x2C, 0x6C, 0xCE));
    private static readonly Brush pFunnelActiveBrush = new SolidColorBrush(Color.FromRgb(0xCE, 0xE1, 0xFB));

    private const double PFunnelBadgeSize = 18;

    private readonly TextBlock pFunnelOrderBadge;
    private readonly Border pFunnelTitleBar;
    private readonly UIElement pFunnelBody;
    private readonly string pFunnelTitleKey;
    private Button? pFunnelFoldButton;
    private bool pFunnelCollapsed;

    public PFunnelRuleFrame(UIElement pBody, string pTitleKey, Action pRemove)
    {
        pFunnelBody = pBody;
        pFunnelTitleKey = pTitleKey;
        pFunnelOrderBadge = new TextBlock
        {
            FontSize = 12,
            FontFamily = pFunnelFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center
        };
        pFunnelTitleBar = PFunnelTitleBuild(pRemove);
    }

    public Border PFunnelHeader => pFunnelTitleBar;

    public void PFunnelOrderSet(int pOrder) => pFunnelOrderBadge.Text = pOrder.ToString();

    public void PFunnelSelectSet(bool pSelected) =>
        pFunnelTitleBar.Background = pSelected ? pFunnelActiveBrush : Brushes.Transparent;

    private Border PFunnelTitleBuild(Action pRemove)
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead(pFunnelTitleKey),
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
            CornerRadius = new CornerRadius(PFunnelBadgeSize / 2),
            Background = pFunnelAccentBrush,
            Padding = new Thickness(6, 0, 6, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            Child = pFunnelOrderBadge
        };

        var pBar = new DockPanel { LastChildFill = true };
        Button pRemoveButton = PFunnelRemoveBuild(pRemove);
        pFunnelFoldButton = PFunnelFoldBuild();
        DockPanel.SetDock(pRemoveButton, Dock.Right);
        DockPanel.SetDock(pFunnelFoldButton, Dock.Right);
        DockPanel.SetDock(pBadge, Dock.Left);
        pBar.Children.Add(pRemoveButton);
        pBar.Children.Add(pFunnelFoldButton);
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

    private Button PFunnelFoldBuild()
    {
        var pButton = new Button
        {
            Width = 20,
            Height = 20,
            Padding = new Thickness(0),
            Cursor = Cursors.Hand,
            Style = PButton.PButtonChromeCreate(false),
            Content = PFunnelGlyphCreate(false),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.Minimize")
        };
        pButton.Click += (_, _) => PFunnelFoldToggle();
        return pButton;
    }

    private void PFunnelFoldToggle()
    {
        pFunnelCollapsed = !pFunnelCollapsed;
        pFunnelBody.Visibility = pFunnelCollapsed ? Visibility.Collapsed : Visibility.Visible;
        pFunnelTitleBar.BorderThickness = new Thickness(0, 0, 0, pFunnelCollapsed ? 0 : 1);

        if (pFunnelFoldButton is { } pFold)
        {
            pFold.Content = PFunnelGlyphCreate(pFunnelCollapsed);
            pFold.ToolTip = LLocalization.LLocalizationTextRead(
                pFunnelCollapsed ? "Inspector.Funnel.Maximize" : "Inspector.Funnel.Minimize");
        }
    }

    private static UIElement PFunnelGlyphCreate(bool pCollapsed)
    {
        var pCanvas = new Canvas
        {
            Width = 12,
            Height = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        if (pCollapsed)
        {
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
        }
        else
        {
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
        }

        return pCanvas;
    }

    private Button PFunnelRemoveBuild(Action pRemove)
    {
        var pButton = new Button
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
            Style = PButton.PButtonChromeCreate(false),
            ToolTip = LLocalization.LLocalizationTextRead("Inspector.Funnel.Remove")
        };
        pButton.Click += (_, _) => pRemove();
        return pButton;
    }
}
