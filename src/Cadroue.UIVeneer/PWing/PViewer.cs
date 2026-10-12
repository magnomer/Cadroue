using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaFramework.MediaRenderer;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed class PViewer : PPanel
{
    private static readonly IReadOnlyDictionary<bool, Action<Config>> pViewerProcessors =
        new Dictionary<bool, Action<Config>>
        {
            [true] = pConfig =>
            {
                pConfig.Video.VideoProcessor = VideoProcessors.Flyleaf;
                pConfig.Video.SyncVPFilters = false;
            },
            [false] = _ => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerOverlayMoves =
        new Dictionary<bool, Action<PViewer>>
        {
            [true] = pViewer =>
            {
                pViewer.pViewerFlyleafHost.Content = null;
                pViewer.pViewerMpvOverlay.PViewerChildSet(pViewer.pViewerOverlayHost);
            },
            [false] = pViewer =>
            {
                pViewer.pViewerMpvOverlay.PViewerChildSet(null);
                pViewer.pViewerFlyleafHost.Content = pViewer.pViewerOverlayHost;
            },
        };

    private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerEngineCreates =
        new Dictionary<bool, Action<PViewer>>
        {
            [true] = pViewer => pViewer.PViewerMpvCreate(),
            [false] = pViewer => pViewer.PViewerFlyleafCreate(),
        };

    private static readonly IReadOnlyDictionary<bool, Action<PViewer>> pViewerLoupeShows =
        new Dictionary<bool, Action<PViewer>>
        {
            [true] = _ => System.Windows.Application.Current.Windows.OfType<PSLoupe>().FirstOrDefault()?.Activate(),
            [false] = pViewer => PSLoupe.PSLoupeShow(
                System.Windows.Application.Current.MainWindow,
                pViewer.QViewer.QViewerMedia.QViewerLoupe.QViewerLoupeCreate()),
        };

    private static readonly IReadOnlyDictionary<QViewerAskKind, Action<PViewer, QViewerAsk, Action<bool>>> pViewerAsks =
        new Dictionary<QViewerAskKind, Action<PViewer, QViewerAsk, Action<bool>>>
        {
            [QViewerAskKind.QViewerAskWarning] = (pViewer, lAsk, pAnswer) =>
            {
                PSWarning.PSWarningShow(Window.GetWindow(pViewer), lAsk.QViewerAskTitle, lAsk.QViewerAskMessage);
                pAnswer(false);
            },
            [QViewerAskKind.QViewerAskDecision] = (pViewer, lAsk, pAnswer) => pAnswer(PSDecision.PSDecisionConfirm(
                Window.GetWindow(pViewer),
                lAsk.QViewerAskTitle,
                lAsk.QViewerAskMessage,
                lAsk.QViewerAskPrimary,
                lAsk.QViewerAskDismiss)),
        };

    private static readonly IReadOnlyDictionary<bool, Func<Microsoft.Win32.OpenFileDialog, string?>> pViewerLocates =
        new Dictionary<bool, Func<Microsoft.Win32.OpenFileDialog, string?>>
        {
            [true] = pDialog => pDialog.FileName,
            [false] = _ => null,
        };

    private readonly Button pViewerCloseButton;
    private readonly Button pViewerPreviewButton;
    private readonly Button pViewerAudioSwitch;
    private readonly Border pViewerEngineSurface;
    private readonly Border pViewerEngineOverlay;
    private readonly Grid pViewerOverlayHost;
    private readonly FlyleafHost pViewerFlyleafHost;
    private readonly PViewerMpvHost pViewerMpvHost;
    private readonly PViewerOverlay pViewerMpvOverlay;
    private readonly IReadOnlyDictionary<bool, FrameworkElement> pViewerHosts;
    private readonly PCrop pViewerOverlay;
    private readonly DispatcherTimer pViewerClockTimer;

    public QViewer QViewer { get; }

    public QPlayer QPlayer => QViewer.QPlayer;

    public PViewer(QViewer lViewer)
        : base("")
    {
        QViewer = lViewer;
        AllowDrop = true;
        Focusable = true;
        FocusVisualStyle = null;

        pViewerOverlay = new PCrop(QViewer);
        pViewerOverlay.DragEnter += PViewerDragAccept;
        pViewerOverlay.DragOver += PViewerDragAccept;
        pViewerOverlay.Drop += PViewerDropHandle;

        pViewerCloseButton = PViewerButtonBuild("/PAsset/PPanel/PViewerClose.svg", "Viewer.Unload.Tooltip", 16);
        pViewerCloseButton.Click += PViewerCloseHandle;
        pViewerPreviewButton = PViewerButtonBuild("/PAsset/PPanel/PViewerPreview.svg", "Viewer.Preview.Tooltip", 44);
        pViewerPreviewButton.Click += PViewerLoupeHandle;
        pViewerAudioSwitch = PViewerAudioBuild();
        pViewerAudioSwitch.Click += PViewerAudioHandle;
        pViewerEngineSurface = PViewerSlotBuild();
        pViewerEngineOverlay = PViewerSlotBuild();

        pViewerOverlayHost = new Grid();
        pViewerOverlayHost.Children.Add(pViewerOverlay);
        pViewerOverlayHost.Children.Add(pViewerCloseButton);
        pViewerOverlayHost.Children.Add(pViewerPreviewButton);
        pViewerOverlayHost.Children.Add(pViewerAudioSwitch);
        pViewerOverlayHost.Children.Add(pViewerEngineOverlay);

        pViewerFlyleafHost = new FlyleafHost
        {
            Content = pViewerOverlayHost,
            VideoBackground = Brushes.White,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None,
            Visibility = Visibility.Collapsed
        };
        pViewerMpvHost = new PViewerMpvHost { Visibility = Visibility.Collapsed };
        pViewerMpvOverlay = new PViewerOverlay(pViewerMpvHost, QViewer.QViewerRenderer);
        pViewerHosts = new Dictionary<bool, FrameworkElement>
        {
            [true] = pViewerMpvHost,
            [false] = pViewerFlyleafHost,
        };

        var pViewerFrame = new Grid();
        pViewerFrame.Children.Add(pViewerFlyleafHost);
        pViewerFrame.Children.Add(pViewerMpvHost);
        pViewerFrame.Children.Add(pViewerEngineSurface);
        Content = new Border
        {
            Margin = PPanelOuterMargin,
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(0),
            Child = pViewerFrame,
            AllowDrop = true,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };

        pViewerClockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        pViewerClockTimer.Tick += PViewerClockHandle;
        QViewer.QViewerPlayback.QViewerClockStarted += pViewerClockTimer.Start;
        QViewer.QViewerPlayback.QViewerClockStopped += pViewerClockTimer.Stop;
        QViewer.QViewerEngineChanged += PViewerAudioUpdate;
        QViewer.QViewerBypassChanged += PViewerBypassHandle;
        QViewer.QViewerRenderer.QViewerHostApplied += PViewerHostApply;
        QViewer.QViewerMedia.QViewerPlayerCreated += PViewerPlayerCreate;
        QViewer.QViewerSource.QViewerSourceAsk += PViewerAskHandle;
        QViewer.QViewerSource.QViewerSourceLocate += PViewerLocateHandle;
        QViewer.QViewerMedia.QViewerFactsAttach(PViewerFactsRead);
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged += PViewerEngineShow;
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged += PViewerEngineHandle;
        PViewerEngineShow();
        PViewerHostApply();

        Loaded += (_, _) => QViewer.QViewerMedia.QViewerHostRecord("panel loaded");
        IsVisibleChanged += (_, pEvent) => QViewer.QViewerMedia.QViewerHostRecord($"panel visible {pEvent.NewValue}");
        pViewerFlyleafHost.Loaded += (_, _) => QViewer.QViewerMedia.QViewerHostRecord("host loaded");
        pViewerFlyleafHost.IsVisibleChanged += (_, pEvent) =>
            QViewer.QViewerMedia.QViewerHostRecord($"host visible {pEvent.NewValue}");
        pViewerFlyleafHost.SizeChanged += (_, pEvent) =>
            QViewer.QViewerMedia.QViewerHostRecord($"host sized {pEvent.NewSize.Width:0}x{pEvent.NewSize.Height:0}");
        QViewer.QViewerMedia.QViewerHostRecord("host built");
    }

    private static Button PViewerButtonBuild(string pIconPath, string pTipKey, double pRightMargin)
    {
        return new Button
        {
            Content = new Image
            {
                Width = 16,
                Height = 16,
                Source = PIcon.PIconRead(pIconPath, new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
                Stretch = Stretch.Uniform
            },
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, pRightMargin, 0),
            ToolTip = LLocalization.LLocalizationTextRead(pTipKey),
            Visibility = Visibility.Collapsed,
            Style = PButton.PButtonPanelCreate()
        };
    }

    private static Button PViewerAudioBuild()
    {
        return new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 52, 0, 0),
            MinWidth = 84,
            Height = 24,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            Visibility = Visibility.Collapsed,
            Style = PButton.PButtonPanelCreate()
        };
    }

    private static Border PViewerSlotBuild()
    {
        return new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 16, 0, 0)
        };
    }

    private void PViewerCloseHandle(object pSender, RoutedEventArgs pEvent) =>
        QViewer.QViewerMedia.QViewerMediaClose(false);

    private void PViewerLoupeHandle(object pSender, RoutedEventArgs pEvent) =>
        pViewerLoupeShows[QViewer.QViewerLoupeActive](this);

    private void PViewerAudioHandle(object pSender, RoutedEventArgs pEvent) => QViewer.QViewerBypassToggle();

    private void PViewerClockHandle(object? pSender, EventArgs pEvent) => QViewer.QViewerPlayback.QViewerTick();

    private void PViewerBypassHandle(bool lBypass) => PViewerAudioUpdate();

    private void PViewerAudioUpdate()
    {
        QViewerSwitch lSwitch = QViewer.QViewerSwitchRead();
        pViewerAudioSwitch.IsEnabled = lSwitch.QViewerSwitchEnabled;
        pViewerAudioSwitch.Content = lSwitch.QViewerSwitchText;
        pViewerAudioSwitch.ToolTip = lSwitch.QViewerSwitchTip;
    }

    private void PViewerEngineShow()
    {
        QViewerEnginePlan lPlan = QViewer.QViewerRenderer.QViewerEngineRead();
        Action<string> lSelect = QViewer.QViewerRenderer.QViewerEngineSelect;
        pViewerEngineSurface.Child = PViewerEngine.PViewerEngineBuild(lPlan, lSelect);
        pViewerEngineOverlay.Child = PViewerEngine.PViewerEngineBuild(lPlan, lSelect);
    }

    private void PViewerEngineHandle() => Dispatcher.BeginInvoke(QViewer.QViewerRenderer.QViewerEngineHandle);

    private void PViewerHostApply()
    {
        pViewerOverlayMoves[QViewer.QViewerMpvActive](this);
        pViewerHosts[true].Visibility = PLook.PLookVisible[QViewer.QViewerMpvShown];
        pViewerHosts[false].Visibility = PLook.PLookVisible[QViewer.QViewerFlyleafShown];
        pViewerCloseButton.Visibility = PLook.PLookVisible[QViewer.QViewerHostVisible];
        pViewerPreviewButton.Visibility = PLook.PLookVisible[QViewer.QViewerHostVisible];
        pViewerAudioSwitch.Visibility = PLook.PLookVisible[QViewer.QViewerAudioShown];
        PViewerAudioUpdate();
        PViewerOverlay.PViewerInertApply(PPlayerFlyleaf.PPlayerHandleRead(pViewerFlyleafHost.Surface));
        PViewerOverlay.PViewerInertApply(PPlayerFlyleaf.PPlayerHandleRead(pViewerFlyleafHost.Overlay));
        pViewerMpvOverlay.PViewerOverlayPlace();
    }

    private void PViewerPlayerCreate() => pViewerEngineCreates[QViewer.QViewerMpvActive](this);

    private void PViewerFlyleafCreate()
    {
        var pConfig = new Config();
        pConfig.Player.KeyBindings.Keys.Clear();
        pViewerProcessors[QViewer.QViewerProcessorForced](pConfig);
        pConfig.Video.BackColor = Colors.White;
        pConfig.Video.ClearScreen = false;
        QPlayer.QPlayerEngineSet(new PPlayerFlyleaf(pViewerFlyleafHost, QPlayer, pConfig).PPlayerSeamRead());
    }

    private void PViewerMpvCreate() =>
        QPlayer.QPlayerMpvCreate(pViewerMpvHost.PViewerHandleRead());

    private QViewerHostFacts PViewerFactsRead()
    {
        Window? pSurface = null;
        nint pHandle = nint.Zero;
        bool pDisposed = false;
        Window? pOverlay = null;
        try
        {
            pDisposed = pViewerFlyleafHost.Disposed;
            pSurface = pViewerFlyleafHost.Surface;
            pHandle = pViewerFlyleafHost.SurfaceHandle;
            pOverlay = pViewerFlyleafHost.Overlay;
        }
        catch
        {
        }

        return new QViewerHostFacts(
            IsVisible,
            pViewerFlyleafHost.IsVisible,
            pSurface?.Visibility.ToString(),
            pSurface?.Width,
            pSurface?.Height,
            pHandle,
            pDisposed,
            pOverlay?.AllowsTransparency);
    }

    private void PViewerAskHandle(QViewerAsk lAsk, Action<bool> lAnswer) =>
        pViewerAsks[lAsk.QViewerAskKind](this, lAsk, lAnswer);

    private static void PViewerLocateHandle(QViewerAsk lAsk, Action<string?> lAnswer)
    {
        var pDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = lAsk.QViewerAskTitle,
            FileName = lAsk.QViewerAskName,
            Filter = lAsk.QViewerAskFilter
        };
        lAnswer(pViewerLocates[pDialog.ShowDialog().GetValueOrDefault()](pDialog));
    }

    private void PViewerDragAccept(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = PLook.PLookDropEffect[QViewer.QViewerSource.QViewerDropResolve(
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pEvent.AllowedEffects.HasFlag(DragDropEffects.Copy))];
        pEvent.Handled = true;
    }

    private void PViewerDropHandle(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = PLook.PLookDropEffect[QViewer.QViewerSource.QViewerDropHandle(
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pEvent.AllowedEffects.HasFlag(DragDropEffects.Copy))];
        pEvent.Handled = true;
    }

    internal bool PViewerSurfaceMatch(nint pForeground)
    {
        try
        {
            return QViewer.QViewerSurfaceMatch(
                pForeground,
                PPlayerFlyleaf.PPlayerHandleRead(pViewerFlyleafHost.Surface),
                PPlayerFlyleaf.PPlayerHandleRead(pViewerFlyleafHost.Overlay));
        }
        catch
        {
            return false;
        }
    }

    public void PViewerClose()
    {
        QViewer.QViewerMedia.QViewerTeardown.QViewerClose();
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged -= PViewerEngineShow;
        Cadroue.Infrastructure.LRenderer.LRendererEngineChanged -= PViewerEngineHandle;
        pViewerClockTimer.Tick -= PViewerClockHandle;
        pViewerOverlay.PCropClose();
        pViewerOverlay.DragEnter -= PViewerDragAccept;
        pViewerOverlay.DragOver -= PViewerDragAccept;
        pViewerOverlay.Drop -= PViewerDropHandle;
        QViewer.QViewerMedia.QViewerTeardown.QViewerStageRun("flyleaf host", PViewerFlyleafDispose);
        QViewer.QViewerMedia.QViewerTeardown.QViewerStageRun("mpv host", PViewerMpvDispose);
    }

    private void PViewerFlyleafDispose()
    {
        pViewerFlyleafHost.Player = null;
        pViewerFlyleafHost.Content = null;
        ((IDisposable)pViewerFlyleafHost).Dispose();
    }

    private void PViewerMpvDispose()
    {
        pViewerMpvOverlay.PViewerOverlayDispose();
        ((IDisposable)pViewerMpvHost).Dispose();
    }
}
