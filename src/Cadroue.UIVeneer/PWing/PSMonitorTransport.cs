using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

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
        psMonitorPlayButton.Click += (_, _) => psMonitorSource.QSMonitorPlayHandle();
        PSMonitorFaceApply();
        return psMonitorPlayButton;
    }

    private void PSMonitorPlayingApply(bool pPlaying) => PSMonitorFaceApply();

    private void PSMonitorFaceApply()
    {
        QSMonitorFace lFace = psMonitorSource.QSMonitorFaceRead();
        psMonitorPlayImage.Source = PAsset.PIcon.PIconRead(
            $"/PAsset/PCompass/{lFace.QSMonitorFaceIcon}", psMonitorAxisFill);
        psMonitorPlayButton.ToolTip = lFace.QSMonitorFaceTip;
    }

    private UIElement PSMonitorZoomBuild()
    {
        var psZoom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomIncrease.svg",
                "NormalizePreview.ZoomIn",
                psMonitorSource.QSMonitorIncreaseZoom));
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomDecrease.svg",
                "NormalizePreview.ZoomOut",
                psMonitorSource.QSMonitorDecreaseZoom));
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
        QSMonitorScrollbar lScroll = psMonitorSource.QSMonitorScrollRead();
        psMonitorScrollbar.ViewportSize = lScroll.QSMonitorScrollViewport;
        psMonitorScrollbar.Maximum = lScroll.QSMonitorScrollMaximum;
        psMonitorScrollbar.Value = lScroll.QSMonitorScrollValue;
        psMonitorScrollbar.IsEnabled = lScroll.QSMonitorScrollEnabled;
        psMonitorScrollbar.Opacity = lScroll.QSMonitorScrollOpacity;
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
            Foreground = PSField.PSFieldMuted,
            Cursor = Cursors.Hand,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = LLocalization.LLocalizationTextRead(pTooltipKey),
            Margin = new Thickness(PSMonitorRadioLeft, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        psRadio.Checked += (_, _) => psMonitorSource.QSMonitorRadioHandle(pBypass);
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
        psMonitorSource.QSMonitorSeekHandle(
            pCanvas.IsMouseCaptured,
            pEvent.GetPosition(pCanvas).X,
            pCanvas.ActualWidth,
            psMonitorViewer.QViewer.QViewerDuration);
    }

    private void PSMonitorCursorHandle(TimeSpan pCursor) =>
        Dispatcher.BeginInvoke(() => psMonitorSource.QSMonitorCursorSet(pCursor));

    private void PSMonitorCursorApply(TimeSpan pCursor) => PSMonitorHeadPlace();

    private void PSMonitorBypassApply()
    {
        psMonitorBeforeRadio.IsChecked = PLook.PLookChecked[psMonitorSource.QSMonitorBypass];
        psMonitorAfterRadio.IsChecked = PLook.PLookChecked[!psMonitorSource.QSMonitorBypass];
    }

    private void PSMonitorHeadPlace()
    {
        PSMonitorHeadApply(psMonitorBeforeCanvas, psMonitorBeforeHead);
        PSMonitorHeadApply(psMonitorAfterCanvas, psMonitorAfterHead);
    }

    private void PSMonitorHeadApply(Canvas pCanvas, Border pHead)
    {
        QSMonitorHead lHead = psMonitorSource.QSMonitorHeadResolve(
            pCanvas.ActualWidth, psMonitorViewer.QViewer.QViewerDuration);
        pHead.Visibility = PLook.PLookVisible[lHead.QSMonitorHeadShown];
        pHead.Margin = new Thickness(lHead.QSMonitorHeadLeft, 0, 0, 0);
    }
}
