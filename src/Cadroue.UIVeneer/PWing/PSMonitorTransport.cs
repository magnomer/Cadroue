using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

using static Cadroue.UIVeneer.PSField;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSMonitor
{
    private const double PSMonitorRadioLeft = PSMonitorGutter + 8;

    private UIElement PSMonitorTransportBuild()
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
        psMonitorPlayImage = new Image { Width = 18, Height = 18, Stretch = Stretch.Uniform };
        psMonitorPlayButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Content = psMonitorPlayImage
        };
        psMonitorPlayButton.Click += (_, _) => psMonitorSource.LSMonitorPlayHandle();
        PSMonitorFaceApply();
        return psMonitorPlayButton;
    }

    private void PSMonitorPlayingApply(bool pPlaying) => PSMonitorFaceApply();

    private void PSMonitorFaceApply()
    {
        LSMonitorFace lFace = psMonitorSource.LSMonitorFaceRead();
        psMonitorPlayImage.Source = PAsset.PIcon.PIconRead(
            $"/PAsset/PCompass/{lFace.LSMonitorFaceIcon}", psMonitorAxisFill);
        psMonitorPlayButton.ToolTip = lFace.LSMonitorFaceTip;
    }

    private UIElement PSMonitorZoomBuild()
    {
        var psZoom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomIncrease.svg",
                "NormalizePreview.ZoomIn",
                psMonitorSource.LSMonitorIncreaseZoom));
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomDecrease.svg",
                "NormalizePreview.ZoomOut",
                psMonitorSource.LSMonitorDecreaseZoom));
        return psZoom;
    }

    private static Button PSMonitorButtonBuild(string pIconPath, string pTooltipKey, Action pAction)
    {
        var psButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Content = new Image
            {
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Source = PAsset.PIcon.PIconRead(pIconPath, psMonitorAxisFill)
            }
        };
        psButton.Click += (_, _) => pAction();
        return psButton;
    }

    private void PSMonitorZoomApply()
    {
        LSMonitorScroll lScroll = psMonitorSource.LSMonitorScrollRead();
        psMonitorScrollbar.ViewportSize = lScroll.LSMonitorScrollViewport;
        psMonitorScrollbar.Maximum = lScroll.LSMonitorScrollMaximum;
        psMonitorScrollbar.Value = lScroll.LSMonitorScrollValue;
        psMonitorScrollbar.IsEnabled = lScroll.LSMonitorScrollEnabled;
        psMonitorScrollbar.Opacity = lScroll.LSMonitorScrollOpacity;
        PSMonitorUpdate();
    }

    private RadioButton PSMonitorRadioBuild(string pLabelKey, string pTooltipKey, bool pBypass)
    {
        var psRadio = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead(pLabelKey),
            GroupName = "PSMonitorAudio",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSFieldMuted,
            Cursor = Cursors.Hand,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Margin = new Thickness(PSMonitorRadioLeft, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        psRadio.Checked += (_, _) => psMonitorSource.LSMonitorRadioHandle(pBypass);
        return psRadio;
    }

    private void PSMonitorSeekStart(object pSender, MouseButtonEventArgs pEvent)
    {
        ((Canvas)pSender).CaptureMouse();
        PSMonitorSeekHandle(pSender, pEvent);
    }

    private void PSMonitorSeekHandle(object pSender, MouseEventArgs pEvent)
    {
        var pCanvas = (Canvas)pSender;
        psMonitorSource.LSMonitorSeekHandle(
            pCanvas.IsMouseCaptured,
            pEvent.GetPosition(pCanvas).X,
            pCanvas.ActualWidth,
            psMonitorViewer.LViewer.LViewerDuration);
    }

    private void PSMonitorCursorHandle(TimeSpan pCursor) =>
        Dispatcher.BeginInvoke(() => psMonitorSource.LSMonitorCursorSet(pCursor));

    private void PSMonitorCursorApply(TimeSpan pCursor) => PSMonitorHeadPlace();

    private void PSMonitorBypassApply()
    {
        psMonitorBeforeRadio.IsChecked = PLook.PLookChecked[psMonitorSource.LSMonitorBypass];
        psMonitorAfterRadio.IsChecked = PLook.PLookChecked[!psMonitorSource.LSMonitorBypass];
    }

    private void PSMonitorHeadPlace()
    {
        PSMonitorHeadApply(psMonitorBeforeCanvas, psMonitorBeforeHead);
        PSMonitorHeadApply(psMonitorAfterCanvas, psMonitorAfterHead);
    }

    private void PSMonitorHeadApply(Canvas pCanvas, Border pHead)
    {
        LSMonitorHead lHead = psMonitorSource.LSMonitorHeadResolve(
            pCanvas.ActualWidth, psMonitorViewer.LViewer.LViewerDuration);
        pHead.Visibility = PLook.PLookVisible[lHead.LSMonitorHeadShown];
        pHead.Margin = new Thickness(lHead.LSMonitorHeadLeft, 0, 0, 0);
    }
}
