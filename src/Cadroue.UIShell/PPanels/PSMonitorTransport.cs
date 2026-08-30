using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Infrastructure;
using Cadroue.UIShell.PMainWindow;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

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
        psMonitorPlaying = pPlaying;
        string pIcon = pPlaying ? "PCompassPause.svg" : "PCompassPlay.svg";
        string pTooltip = pPlaying ? "NormalizePreview.PauseTooltip" : "NormalizePreview.PlayTooltip";
        psMonitorPlayImage.Source = PAssets.PIcon.PIconRead($"/PAssets/PCompass/{pIcon}", psMonitorAxisFill);
        psMonitorPlayButton.ToolTip = LLocalization.LLocalizationTextRead(pTooltip);
    }

    private void PSMonitorPlayToggle()
    {
        if (psMonitorPlaying)
        {
            psMonitorFlow.PFlowPauseRaise();
        }
        else
        {
            psMonitorFlow.PFlowPlayRaise();
        }
    }

    private void PSMonitorPlayingHandle(bool pPlaying)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorPlayingHandle(pPlaying));
            return;
        }

        PSMonitorPlayingApply(pPlaying);
    }

    private UIElement PSMonitorZoomBuild()
    {
        var psZoom = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        psZoom.Children.Add(PSMonitorButtonBuild(
            "/PAssets/PCompass/PCompassZoomIncrease.svg", "NormalizePreview.ZoomIn", () => PSMonitorZoomApply(PSMonitorZoomStep)));
        psZoom.Children.Add(PSMonitorButtonBuild(
            "/PAssets/PCompass/PCompassZoomDecrease.svg", "NormalizePreview.ZoomOut", () => PSMonitorZoomApply(1 / PSMonitorZoomStep)));
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
                Source = PAssets.PIcon.PIconRead(pIconPath, psMonitorAxisFill)
            }
        };
        psButton.Click += (_, _) => pAction();
        return psButton;
    }

    private void PSMonitorZoomApply(double pFactor)
    {
        double pCenter = psMonitorOffset + 1.0 / psMonitorScale / 2;
        psMonitorScale = Math.Clamp(psMonitorScale * pFactor, 1, PSMonitorZoomMost);
        double pViewport = 1.0 / psMonitorScale;
        psMonitorOffset = Math.Clamp(pCenter - pViewport / 2, 0, 1 - pViewport);
        PSMonitorScrollbarApply();
        PSMonitorUpdate();
    }

    private void PSMonitorScrollbarApply()
    {
        double pViewport = 1.0 / psMonitorScale;
        psMonitorScrollbar.ViewportSize = pViewport;
        psMonitorScrollbar.Maximum = 1 - pViewport;
        psMonitorScrollbar.Value = psMonitorOffset;
        psMonitorScrollbar.IsEnabled = psMonitorScale > 1;
        psMonitorScrollbar.Opacity = psMonitorScale > 1 ? 1 : 0.35;
    }

    private void PSMonitorScrollbarHandle(object pSender, System.Windows.RoutedPropertyChangedEventArgs<double> pEvent)
    {
        psMonitorOffset = pEvent.NewValue;
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
            Margin = new Thickness(PSMonitorGutter + 8, 8, 12, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        psRadio.Checked += (_, _) =>
        {
            if (!psMonitorRadioProgram)
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

        double pLocal = Math.Clamp((pX - PSMonitorGutter) / pPlot, 0, 1);
        double pFraction = Math.Clamp(psMonitorOffset + pLocal / psMonitorScale, 0, 1);
        psMonitorFlow.PFlowCursorSeek(TimeSpan.FromSeconds(pFraction * pDuration));
    }

    private void PSMonitorCursorHandle(TimeSpan pCursor)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(() => PSMonitorCursorHandle(pCursor));
            return;
        }

        psMonitorCursor = pCursor;
        PSMonitorHeadPlace();
    }

    private void PSMonitorBypassHandle(bool pBypass)
    {
        psMonitorRadioProgram = true;
        psMonitorBeforeRadio.IsChecked = pBypass;
        psMonitorAfterRadio.IsChecked = !pBypass;
        psMonitorRadioProgram = false;
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

        double pFraction = Math.Clamp(psMonitorCursor.TotalSeconds / pDuration, 0, 1);
        double pLocal = (pFraction - psMonitorOffset) * psMonitorScale;
        if (pLocal < 0 || pLocal > 1)
        {
            pHead.Visibility = Visibility.Collapsed;
            return;
        }

        pHead.Visibility = Visibility.Visible;
        pHead.Margin = new Thickness(PSMonitorGutter + pLocal * pPlot, 0, 0, 0);
    }
}
