using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
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
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            Content = psMonitorPlayImage
        };
    }

    private void PSMonitorPlayingApply(bool pPlaying) => PSMonitorFaceApply();

    private void PSMonitorFaceApply()
    {
        QSMonitorFace lFace = psMonitorSource.QSMonitorFaceRead();
        PSMonitorFaceSet(PSMonitorIconFormat(lFace.QSMonitorFaceIcon), lFace.QSMonitorFaceTip);
    }

    private static string PSMonitorIconFormat(string pIcon) => $"/PAsset/PCompass/{pIcon}";

    private void PSMonitorFaceSet(string pIconPath, string pTip)
    {
        psMonitorPlayImage.Source = QIcon.QIconRead(pIconPath, psMonitorAxisFill);
        psMonitorPlayButton.ToolTip = pTip;
    }

    private UIElement PSMonitorZoomBuild()
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
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = Cursors.Hand,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey),
            Content = new Image
            {
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform,
                Source = QIcon.QIconRead(pIconPath, psMonitorAxisFill)
            }
        };
    }

    private void PSMonitorZoomApply()
    {
        QSMonitorScrollbar lScroll = psMonitorSource.QSMonitorViewport.QSMonitorScrollRead();
        PSMonitorScrollSet(lScroll);
        PSMonitorUpdate();
    }

    private void PSMonitorScrollSet(QSMonitorScrollbar lScroll)
    {
        psMonitorScrollbar.ViewportSize = lScroll.QSMonitorScrollViewport;
        psMonitorScrollbar.Maximum = lScroll.QSMonitorScrollMaximum;
        psMonitorScrollbar.Value = lScroll.QSMonitorScrollValue;
        psMonitorScrollbar.IsEnabled = lScroll.QSMonitorScrollEnabled;
        psMonitorScrollbar.Opacity = lScroll.QSMonitorScrollOpacity;
    }

    private RadioButton PSMonitorRadioBuild(string pLabelKey, string pTooltipKey, bool pBypass)
    {
        RadioButton psRadio = PSMonitorOptionBuild(pLabelKey, pTooltipKey);
        PSMonitorRadioAttach(psRadio, pBypass);
        return psRadio;
    }

    private void PSMonitorRadioAttach(RadioButton psRadio, bool pBypass) =>
        psRadio.Checked += (_, _) => psMonitorSource.QSMonitorRadioHandle(pBypass);

    private static RadioButton PSMonitorOptionBuild(string pLabelKey, string pTooltipKey)
    {
        return new RadioButton
        {
            Content = QLocalizationCatalog.QLocalizationTextRead(pLabelKey),
            GroupName = "PSMonitorAudio",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldMuted,
            Cursor = Cursors.Hand,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead(pTooltipKey),
            Margin = new Thickness(PSMonitorRadioLeft, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
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
