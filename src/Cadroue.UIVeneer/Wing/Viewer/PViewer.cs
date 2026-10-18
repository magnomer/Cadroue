using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PViewer : UserControl
{
    private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerOverlayMoves =
        new Dictionary<bool, Action<PViewer>>
        {
            [true] = pViewer =>
            {
                pViewer.pViewerFlyleaf.PViewerFlyleafHost.Content = null;
                pViewer.pViewerMpvOverlay.PViewerChildSet(pViewer.pViewerOverlayHost);
            },
            [false] = pViewer =>
            {
                pViewer.pViewerMpvOverlay.PViewerChildSet(null);
                pViewer.pViewerFlyleaf.PViewerFlyleafHost.Content = pViewer.pViewerOverlayHost;
            },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerEngineCreates =
        new Dictionary<bool, Action<PViewer>>
        {
            [true] = pViewer => pViewer.PViewerMpvCreate(),
            [false] = pViewer => pViewer.pViewerFlyleaf.PViewerFlyleafCreate(),
        };

    private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerLoupeShows =
        new Dictionary<bool, Action<PViewer>>
        {
            [true] = _ => System.Windows.Application.Current.Windows.OfType<PSLoupe>().FirstOrDefault()?.Activate(),
            [false] = pViewer => PSLoupe.PSLoupeShow(
                System.Windows.Application.Current.MainWindow,
                pViewer.QViewer.QViewerMedia.QViewerLoupe.QViewerLoupeCreate()),
        };

    private readonly Button pViewerCloseButton;
    private readonly Button pViewerPreviewButton;
    private readonly Button pViewerAudioSwitch;
    private readonly Border pViewerEngineSurface;
    private readonly Border pViewerEngineOverlay;
    private readonly Grid pViewerOverlayHost;
    private readonly PViewerFlyleaf pViewerFlyleaf;
    private readonly PViewerMpvHost pViewerMpvHost;
    private readonly PViewerOverlay pViewerMpvOverlay;
    private readonly IReadOnlyDictionary<bool, FrameworkElement> pViewerHosts;
    private readonly PCrop pViewerOverlay;
    private readonly DispatcherTimer pViewerClockTimer;

    public QViewer QViewer { get; }

    public QPlayer QPlayer => QViewer.QPlayer;

    public PViewer(QViewer lViewer)
    {
        QPanel.QPanelAttach(this);
        QViewer = lViewer;
        PViewerInputApply();
        pViewerOverlay = new PCrop(QViewer);
        PViewerCropAttach();
        pViewerCloseButton = PViewerControl.PViewerButtonBuild(
            "/PAsset/PPanel/PViewerClose.svg", "Viewer.Unload.Tooltip", 16);
        pViewerPreviewButton = PViewerControl.PViewerButtonBuild(
            "/PAsset/PPanel/PViewerPreview.svg", "Viewer.Preview.Tooltip", 44);
        pViewerAudioSwitch = PViewerControl.PViewerAudioBuild();
        PViewerButtonsAttach();
        pViewerEngineSurface = PViewerControl.PViewerSlotBuild();
        pViewerEngineOverlay = PViewerControl.PViewerSlotBuild();
        pViewerOverlayHost = PViewerLayerBuild();
        pViewerFlyleaf = new PViewerFlyleaf(QViewer, pViewerOverlayHost);
        pViewerMpvHost = PViewerMpvBuild();
        pViewerMpvOverlay = new PViewerOverlay(pViewerMpvHost, QViewer.QViewerRenderer);
        pViewerHosts = PViewerHostsCreate();
        PViewerFrameBuild();
        pViewerClockTimer = PViewerClockCreate();
        PViewerHandlerAttach();
        PViewerEngineShow();
        PViewerHostApply();
        PViewerTraceAttach();
        QViewer.QViewerHost.QViewerHostRecord("host built");
    }

    private void PViewerInputApply()
    {
        AllowDrop = true;
        Focusable = true;
        FocusVisualStyle = null;
    }

    private void PViewerCropAttach()
    {
        pViewerOverlay.DragEnter += PViewerDragAccept;
        pViewerOverlay.DragOver += PViewerDragAccept;
        pViewerOverlay.Drop += PViewerDropHandle;
    }

    private void PViewerCropDetach()
    {
        pViewerOverlay.DragEnter -= PViewerDragAccept;
        pViewerOverlay.DragOver -= PViewerDragAccept;
        pViewerOverlay.Drop -= PViewerDropHandle;
    }

    private void PViewerButtonsAttach()
    {
        pViewerCloseButton.Click += PViewerCloseHandle;
        pViewerPreviewButton.Click += PViewerLoupeHandle;
        pViewerAudioSwitch.Click += PViewerAudioHandle;
    }

    private Grid PViewerLayerBuild()
    {
        var pLayer = new Grid();
        pLayer.Children.Add(pViewerOverlay);
        pLayer.Children.Add(pViewerCloseButton);
        pLayer.Children.Add(pViewerPreviewButton);
        pLayer.Children.Add(pViewerAudioSwitch);
        pLayer.Children.Add(pViewerEngineOverlay);
        return pLayer;
    }

    private static PViewerMpvHost PViewerMpvBuild() => new() { Visibility = Visibility.Collapsed };

    private Dictionary<bool, FrameworkElement> PViewerHostsCreate() => new()
    {
        [true] = pViewerMpvHost,
        [false] = pViewerFlyleaf.PViewerFlyleafHost,
    };

    private void PViewerFrameBuild()
    {
        var pViewerFrame = new Grid();
        pViewerFrame.Children.Add(pViewerFlyleaf.PViewerFlyleafHost);
        pViewerFrame.Children.Add(pViewerMpvHost);
        pViewerFrame.Children.Add(pViewerEngineSurface);
        Content = new Border
        {
            Margin = QPanel.QPanelMarginRead(),
            BorderBrush = QPanel.QPanelLineRead(),
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(0),
            Child = pViewerFrame,
            AllowDrop = true,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };
    }

    private static DispatcherTimer PViewerClockCreate() => new() { Interval = TimeSpan.FromMilliseconds(100) };

    private void PViewerHandlerAttach()
    {
        pViewerClockTimer.Tick += PViewerClockHandle;
        QViewer.QViewerPlayback.QViewerClockStarted += pViewerClockTimer.Start;
        QViewer.QViewerPlayback.QViewerClockStopped += pViewerClockTimer.Stop;
        QViewer.QViewerEngineChanged += PViewerAudioUpdate;
        QViewer.QViewerAudio.QViewerBypassChanged += PViewerBypassHandle;
        QViewer.QViewerRenderer.QViewerHostApplied += PViewerHostApply;
        QViewer.QViewerMedia.QViewerPlayerCreated += PViewerPlayerCreate;
        QViewer.QViewerSource.QViewerSidecar.QViewerSourceAsk += PViewerAskHandle;
        QViewer.QViewerSource.QViewerSidecar.QViewerSourceLocate += PViewerAsk.PViewerAskOpen;
        QViewer.QViewerHost.QViewerFactsAttach(() => pViewerFlyleaf.PViewerFactsRead(IsVisible));
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged += PViewerEngineShow;
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged += PViewerEngineHandle;
    }

    private void PViewerHandlerDetach()
    {
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged -= PViewerEngineShow;
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged -= PViewerEngineHandle;
        pViewerClockTimer.Tick -= PViewerClockHandle;
    }

    private void PViewerTraceAttach()
    {
        Loaded += (_, _) => QViewer.QViewerHost.QViewerHostRecord("panel loaded");
        IsVisibleChanged += (_, pEvent) => QViewer.QViewerHost.QViewerHostRecord($"panel visible {pEvent.NewValue}");
        pViewerFlyleaf.PViewerTraceAttach();
    }

    private void PViewerCloseHandle(object pSender, RoutedEventArgs pEvent) =>
        QViewer.QViewerMedia.QViewerMediaClose(false);

    private void PViewerLoupeHandle(object pSender, RoutedEventArgs pEvent) =>
        pViewerLoupeShows[QViewer.QViewerLoupeActive](this);

    private void PViewerAudioHandle(object pSender, RoutedEventArgs pEvent) =>
        QViewer.QViewerAudio.QViewerBypassToggle();

    private void PViewerClockHandle(object? pSender, EventArgs pEvent) => QViewer.QViewerPlayback.QViewerTick();

    private void PViewerBypassHandle(bool lBypass) => PViewerAudioUpdate();

    private void PViewerAudioUpdate()
    {
        QViewerSwitch lSwitch = QViewer.QViewerAudio.QViewerSwitchRead();
        pViewerAudioSwitch.IsEnabled = lSwitch.QViewerSwitchEnabled;
        pViewerAudioSwitch.Content = lSwitch.QViewerSwitchText;
        pViewerAudioSwitch.ToolTip = lSwitch.QViewerSwitchTip;
    }

    private void PViewerEngineShow()
    {
        QViewerEnginePlan lPlan = QViewerEnginePreference.QViewerEngineRead();
        Action<string> lSelect = QViewerEnginePreference.QViewerEngineSelect;
        pViewerEngineSurface.Child = PViewerEngine.PViewerEngineBuild(lPlan, lSelect);
        pViewerEngineOverlay.Child = PViewerEngine.PViewerEngineBuild(lPlan, lSelect);
    }

    private void PViewerEngineHandle() => Dispatcher.BeginInvoke(QViewer.QViewerRenderer.QViewerEngineHandle);

    private void PViewerHostApply()
    {
        pViewerOverlayMoves[QViewer.QViewerMpvActive](this);
        PViewerShownApply();
        PViewerAudioUpdate();
        pViewerFlyleaf.PFlyleafInertApply();
        pViewerMpvOverlay.PViewerOverlayPlace();
    }

    private void PViewerShownApply()
    {
        pViewerHosts[true].Visibility = PLook.PLookVisible[QViewer.QViewerMpvShown];
        pViewerHosts[false].Visibility = PLook.PLookVisible[QViewer.QViewerFlyleafShown];
        pViewerCloseButton.Visibility = PLook.PLookVisible[QViewer.QViewerHostVisible];
        pViewerPreviewButton.Visibility = PLook.PLookVisible[QViewer.QViewerHostVisible];
        pViewerAudioSwitch.Visibility = PLook.PLookVisible[QViewer.QViewerAudioShown];
    }

    private void PViewerPlayerCreate() => pViewerEngineCreates[QViewer.QViewerMpvActive](this);

    private void PViewerMpvCreate() =>
        QPlayer.QPlayerMpvCreate(pViewerMpvHost.PViewerHandleRead());

    private void PViewerAskHandle(QViewerAsk lAsk, Action<bool> lAnswer) =>
        PViewerAsk.PViewerAskShow(this, lAsk, lAnswer);

    private void PViewerDragAccept(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = PLook.PLookDropEffect[QViewer.QViewerSource.QViewerDrop.QViewerDropResolve(
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pEvent.AllowedEffects.HasFlag(DragDropEffects.Copy))];
        pEvent.Handled = true;
    }

    private void PViewerDropHandle(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = PLook.PLookDropEffect[QViewer.QViewerSource.QViewerDrop.QViewerDropHandle(
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pEvent.AllowedEffects.HasFlag(DragDropEffects.Copy))];
        pEvent.Handled = true;
    }

    internal bool PViewerSurfaceMatch(nint pForeground) => pViewerFlyleaf.PViewerSurfaceMatch(pForeground);

    public void PViewerClose()
    {
        QViewer.QViewerMedia.QViewerTeardown.QViewerClose();
        PViewerHandlerDetach();
        pViewerOverlay.PCropClose();
        PViewerCropDetach();
        QViewer.QViewerMedia.QViewerTeardown.QViewerStageRun("flyleaf host", pViewerFlyleaf.PViewerFlyleafDispose);
        QViewer.QViewerMedia.QViewerTeardown.QViewerStageRun("mpv host", PViewerMpvDispose);
    }

    private void PViewerMpvDispose()
    {
        pViewerMpvOverlay.PViewerOverlayDispose();
        ((IDisposable)pViewerMpvHost).Dispose();
    }
}
