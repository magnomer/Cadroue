using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

// The monitor's command buttons: play and pause, and the zoom pair, each wired to the monitor driver.
internal sealed class PSMonitorTransport
{
    private readonly QSMonitor psMonitorSource;

    private Image psMonitorPlayImage = null!;
    private Button psMonitorPlayButton = null!;

    internal PSMonitorTransport(QSMonitor pSource) => psMonitorSource = pSource;

    internal UIElement PSMonitorTransportBuild()
    {
        var psTransport = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        };
        psTransport.Children.Add(PSMonitorPlayBuild());
        return psTransport;
    }

    private Button PSMonitorPlayBuild()
    {
        PSMonitorFaceBuild();
        PSMonitorPlayAttach();
        PSMonitorFaceApply();
        return psMonitorPlayButton;
    }

    private void PSMonitorPlayAttach() =>
        psMonitorPlayButton.Click += (_, _) => psMonitorSource.QSMonitorPlayHandle();

    private void PSMonitorFaceBuild()
    {
        psMonitorPlayImage = new Image { Width = 18, Height = 18, Stretch = Stretch.Uniform };
        psMonitorPlayButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = PSMonitorRail.PSMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Content = psMonitorPlayImage
        };
    }

    internal void PSMonitorPlayingApply(bool pPlaying) => PSMonitorFaceApply();

    private void PSMonitorFaceApply()
    {
        QSMonitorFace lFace = psMonitorSource.QSMonitorFaceRead();
        PSMonitorFaceSet(PSMonitorIconFormat(lFace.QSMonitorFaceIcon), lFace.QSMonitorFaceTip);
    }

    private static string PSMonitorIconFormat(string pIcon) => $"/PAsset/PCompass/{pIcon}";

    private void PSMonitorFaceSet(string pIconPath, string pTip)
    {
        psMonitorPlayImage.Source = QIcon.QIconRead(pIconPath, PSMonitorRail.PSMonitorAxisFill);
        psMonitorPlayButton.ToolTip = pTip;
    }

    internal UIElement PSMonitorZoomBuild()
    {
        var psZoom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomIncrease.svg",
                "NormalizePreview.ZoomIn",
                psMonitorSource.QSMonitorViewport.QSMonitorIncreaseZoom));
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomDecrease.svg",
                "NormalizePreview.ZoomOut",
                psMonitorSource.QSMonitorViewport.QSMonitorDecreaseZoom));
        return psZoom;
    }

    private static Button PSMonitorButtonBuild(string pIconPath, string pTooltipKey, Action pAction)
    {
        Button psButton = PSMonitorTileBuild(pIconPath, pTooltipKey);
        PSMonitorClickAttach(psButton, pAction);
        return psButton;
    }

    private static void PSMonitorClickAttach(Button psButton, Action pAction) => psButton.Click += (_, _) => pAction();

    private static Button PSMonitorTileBuild(string pIconPath, string pTooltipKey)
    {
        return new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = PSMonitorRail.PSMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey),
            Content = new Image
            {
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Source = QIcon.QIconRead(pIconPath, PSMonitorRail.PSMonitorAxisFill)
            }
        };
    }
}
