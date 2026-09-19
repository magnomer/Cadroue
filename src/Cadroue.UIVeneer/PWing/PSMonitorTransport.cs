using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

using static Cadroue.UIVeneer.PSField;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSMonitor
{
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
        psMonitorPlayButton.Click += (_, _) => PSMonitorPlayToggle();
        PSMonitorPlayingApply(false);
        return psMonitorPlayButton;
    }

    private void PSMonitorPlayingApply(bool pPlaying)
    {
        string pIcon = pPlaying ? "PCompassPause.svg" : "PCompassPlay.svg";
        string pTooltip = pPlaying ? "NormalizePreview.PauseTooltip" : "NormalizePreview.PlayTooltip";
        psMonitorPlayImage.Source = PAsset.PIcon.PIconRead($"/PAsset/PCompass/{pIcon}", psMonitorAxisFill);
        psMonitorPlayButton.ToolTip = LLocalization.LLocalizationTextRead(pTooltip);
    }

    private void PSMonitorPlayToggle()
    {
        if (psMonitorSource.LSMonitorPlaying)
        {
            psMonitorFlow.LFlow.LFlowPauseRaise();
        }
        else
        {
            psMonitorFlow.LFlow.LFlowPlayRaise();
        }
    }

    private void PSMonitorPlayingHandle(bool pPlaying)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorPlayingHandle(pPlaying));
            return;
        }

        psMonitorSource.LSMonitorPlayingSet(pPlaying);
    }

    private UIElement PSMonitorZoomBuild()
    {
        var psZoom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomIncrease.svg",
                "NormalizePreview.ZoomIn",
                () => psMonitorSource.LSMonitorZoom(PSMonitorZoomStep, PSMonitorZoomMost)));
        psZoom.Children.Add(
            PSMonitorButtonBuild(
                "/PAsset/PCompass/PCompassZoomDecrease.svg",
                "NormalizePreview.ZoomOut",
                () => psMonitorSource.LSMonitorZoom(1 / PSMonitorZoomStep, PSMonitorZoomMost)));
        return psZoom;
    }

    private Button PSMonitorButtonBuild(string pIconPath, string pTooltipKey, Action pAction)
    {
        var psButton = new Button
        {
            Width = 34,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = psMonitorGridFill,
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
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
        double pScale = psMonitorSource.LSMonitorScale;
        double pViewport = 1.0 / pScale;
        psMonitorScrollbar.ViewportSize = pViewport;
        psMonitorScrollbar.Maximum = 1 - pViewport;
        psMonitorScrollbar.Value = psMonitorSource.LSMonitorOffset;
        psMonitorScrollbar.IsEnabled = pScale > 1;
        psMonitorScrollbar.Opacity = pScale > 1 ? 1 : 0.35;
        PSMonitorUpdate();
    }

    private void PSMonitorScrollbarHandle(object pSender, System.Windows.RoutedPropertyChangedEventArgs<double> pEvent)
    {
        if (Math.Abs(pEvent.NewValue - psMonitorSource.LSMonitorOffset) > double.Epsilon)
        {
            psMonitorSource.LSMonitorOffsetSet(pEvent.NewValue);
        }
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
            Margin = new Thickness(PSMonitorGutter + 8, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        psRadio.Checked += (_, _) =>
        {
            if (!psMonitorSource.LSMonitorRadioProgram)
            {
                psMonitorViewer.PViewerBypassSet(pBypass);
            }
        };
        return psRadio;
    }

    private void PSMonitorSeekStart(Canvas pCanvas, MouseButtonEventArgs pEvent)
    {
        pCanvas.CaptureMouse();
        PSMonitorSeekApply(pCanvas, pEvent.GetPosition(pCanvas).X);
    }

    private void PSMonitorSeekMove(Canvas pCanvas, MouseEventArgs pEvent)
    {
        if (pCanvas.IsMouseCaptured)
        {
            PSMonitorSeekApply(pCanvas, pEvent.GetPosition(pCanvas).X);
        }
    }

    private void PSMonitorSeekApply(Canvas pCanvas, double pX)
    {
        double pPlot = pCanvas.ActualWidth - PSMonitorGutter;
        double pDuration = psMonitorViewer.PViewerDurationRead().TotalSeconds;
        if (pPlot <= 1 || pDuration <= 0)
        {
            return;
        }

        double pFraction = psMonitorSource.LSMonitorFractionResolve((pX - PSMonitorGutter) / pPlot);
        psMonitorFlow.LFlow.LFlowCursorSeek(TimeSpan.FromSeconds(pFraction * pDuration));
    }

    private void PSMonitorCursorHandle(TimeSpan pCursor)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorCursorHandle(pCursor));
            return;
        }

        psMonitorSource.LSMonitorCursorSet(pCursor);
    }

    private void PSMonitorCursorApply(TimeSpan pCursor) => PSMonitorHeadPlace();

    private void PSMonitorBypassHandle(bool pBypass)
    {
        psMonitorSource.LSMonitorRadioSet(true);
        psMonitorBeforeRadio.IsChecked = pBypass;
        psMonitorAfterRadio.IsChecked = !pBypass;
        psMonitorSource.LSMonitorRadioSet(false);
    }

    private void PSMonitorHeadPlace()
    {
        PSMonitorHeadApply(psMonitorBeforeCanvas, psMonitorBeforeHead);
        PSMonitorHeadApply(psMonitorAfterCanvas, psMonitorAfterHead);
    }

    private void PSMonitorHeadApply(Canvas pCanvas, Border pHead)
    {
        double pPlot = pCanvas.ActualWidth - PSMonitorGutter;
        double pDuration = psMonitorViewer.PViewerDurationRead().TotalSeconds;
        if (pPlot <= 1 || pDuration <= 0)
        {
            pHead.Visibility = Visibility.Collapsed;
            return;
        }

        double pFraction = Math.Clamp(psMonitorSource.LSMonitorCursor.TotalSeconds / pDuration, 0, 1);
        double pLocal = psMonitorSource.LSMonitorLocalResolve(pFraction);
        if (pLocal < 0 || pLocal > 1)
        {
            pHead.Visibility = Visibility.Collapsed;
            return;
        }

        pHead.Visibility = Visibility.Visible;
        pHead.Margin = new Thickness(PSMonitorGutter + pLocal * pPlot, 0, 0, 0);
    }
}
