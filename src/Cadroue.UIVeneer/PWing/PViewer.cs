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
            [false] = pViewer => PSLoupe.PSLoupeShow(System.Windows.Application.Current.MainWindow, pViewer.LViewer),
        };

    private static readonly IReadOnlyDictionary<LViewerAskKind, Action<PViewer, LViewerAsk, Action<bool>>> pViewerAsks =
        new Dictionary<LViewerAskKind, Action<PViewer, LViewerAsk, Action<bool>>>
        {
            [LViewerAskKind.LViewerAskWarning] = (pViewer, lAsk, pAnswer) =>
            {
                PSWarning.PSWarningShow(Window.GetWindow(pViewer), lAsk.LViewerAskTitle, lAsk.LViewerAskMessage);
                pAnswer(false);
            },
            [LViewerAskKind.LViewerAskDecision] = (pViewer, lAsk, pAnswer) => pAnswer(PSDecision.PSDecisionConfirm(
                Window.GetWindow(pViewer),
                lAsk.LViewerAskTitle,
                lAsk.LViewerAskMessage,
                lAsk.LViewerAskPrimary,
                lAsk.LViewerAskDismiss)),
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

    public LViewer LViewer { get; } = new();

    public LPlayer LPlayer => LViewer.LPlayer;

    public PViewer(bool pAudioEligible = false, bool pEditEligible = false, bool pColorPreview = false) : base("")
    {
        LViewer.LViewerEligibleSet(pAudioEligible, pEditEligible, pColorPreview);
        AllowDrop = true;
        Focusable = true;
        FocusVisualStyle = null;

        pViewerOverlay = new PCrop(LViewer);
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
        pViewerMpvOverlay = new PViewerOverlay(pViewerMpvHost, LViewer.LViewerRenderer);
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
        LViewer.LViewerPlayback.LViewerClockStart += pViewerClockTimer.Start;
        LViewer.LViewerPlayback.LViewerClockStop += pViewerClockTimer.Stop;
        LViewer.LViewerEngineChange += PViewerAudioUpdate;
        LViewer.LViewerBypassChange += PViewerBypassHandle;
        LViewer.LViewerRenderer.LViewerHostApply += PViewerHostApply;
        LViewer.LViewerMedia.LViewerPlayerCreate += PViewerPlayerCreate;
        LViewer.LViewerSource.LViewerSourceAsk += PViewerAskHandle;
        LViewer.LViewerSource.LViewerSourceLocate += PViewerLocateHandle;
        LViewer.LViewerMedia.LViewerFactsAttach(PViewerFactsRead);
        Cadroue.Infrastructure.LRenderer.LRendererEngineChange += PViewerEngineShow;
        Cadroue.Infrastructure.LRenderer.LRendererEngineChange += PViewerEngineHandle;
        PViewerEngineShow();
        PViewerHostApply();

        Loaded += (_, _) => LViewer.LViewerMedia.LViewerHostRecord("panel loaded");
        IsVisibleChanged += (_, pEvent) => LViewer.LViewerMedia.LViewerHostRecord($"panel visible {pEvent.NewValue}");
        pViewerFlyleafHost.Loaded += (_, _) => LViewer.LViewerMedia.LViewerHostRecord("host loaded");
        pViewerFlyleafHost.IsVisibleChanged += (_, pEvent) =>
            LViewer.LViewerMedia.LViewerHostRecord($"host visible {pEvent.NewValue}");
        pViewerFlyleafHost.SizeChanged += (_, pEvent) =>
            LViewer.LViewerMedia.LViewerHostRecord($"host sized {pEvent.NewSize.Width:0}x{pEvent.NewSize.Height:0}");
        LViewer.LViewerMedia.LViewerHostRecord("host built");
    }

    private static Button PViewerButtonBuild(string pIconPath, string pTipKey, double pRightMargin)
    {
        return new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
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
        LViewer.LViewerMedia.LViewerMediaClose(false);

    private void PViewerLoupeHandle(object pSender, RoutedEventArgs pEvent) =>
        pViewerLoupeShows[LViewer.LViewerLoupeActive](this);

    private void PViewerAudioHandle(object pSender, RoutedEventArgs pEvent) => LViewer.LViewerBypassToggle();

    private void PViewerClockHandle(object? pSender, EventArgs pEvent) => LViewer.LViewerPlayback.LViewerTick();

    private void PViewerBypassHandle(bool lBypass) => PViewerAudioUpdate();

    private void PViewerAudioUpdate()
    {
        LViewerSwitch lSwitch = LViewer.LViewerSwitchRead();
        pViewerAudioSwitch.IsEnabled = lSwitch.LViewerSwitchEnabled;
        pViewerAudioSwitch.Content = lSwitch.LViewerSwitchText;
        pViewerAudioSwitch.ToolTip = lSwitch.LViewerSwitchTip;
    }

    private void PViewerEngineShow()
    {
        LViewerEnginePlan lPlan = LViewer.LViewerRenderer.LViewerEngineRead();
        Action<string> lSelect = LViewer.LViewerRenderer.LViewerEngineSelect;
        pViewerEngineSurface.Child = PViewerEngine.PViewerEngineBuild(lPlan, lSelect);
        pViewerEngineOverlay.Child = PViewerEngine.PViewerEngineBuild(lPlan, lSelect);
    }

    private void PViewerEngineHandle() => Dispatcher.BeginInvoke(LViewer.LViewerRenderer.LViewerEngineHandle);

    private void PViewerHostApply()
    {
        pViewerOverlayMoves[LViewer.LViewerMpvActive](this);
        pViewerHosts[true].Visibility = PLook.PLookVisible[LViewer.LViewerMpvShown];
        pViewerHosts[false].Visibility = PLook.PLookVisible[LViewer.LViewerFlyleafShown];
        pViewerCloseButton.Visibility = PLook.PLookVisible[LViewer.LViewerHostVisible];
        pViewerPreviewButton.Visibility = PLook.PLookVisible[LViewer.LViewerHostVisible];
        pViewerAudioSwitch.Visibility = PLook.PLookVisible[LViewer.LViewerAudioShown];
        PViewerAudioUpdate();
        PViewerOverlay.PViewerInertApply(PPlayerFlyleaf.PPlayerHandleRead(pViewerFlyleafHost.Surface));
        PViewerOverlay.PViewerInertApply(PPlayerFlyleaf.PPlayerHandleRead(pViewerFlyleafHost.Overlay));
        pViewerMpvOverlay.PViewerOverlayPlace();
    }

    private void PViewerPlayerCreate() => pViewerEngineCreates[LViewer.LViewerMpvActive](this);

    private void PViewerFlyleafCreate()
    {
        var pConfig = new Config();
        pConfig.Player.KeyBindings.Keys.Clear();
        pViewerProcessors[LViewer.LViewerProcessorForced](pConfig);
        pConfig.Video.BackColor = Colors.White;
        pConfig.Video.ClearScreen = false;
        LPlayer.LPlayerEngineSet(new PPlayerFlyleaf(pViewerFlyleafHost, LPlayer, pConfig).PPlayerSeamRead());
    }

    private void PViewerMpvCreate() =>
        LPlayer.LPlayerEngineSet(new LPlayerMpv(pViewerMpvHost.PViewerHandleRead()).LPlayerSeamRead());

    private LViewerHostFacts PViewerFactsRead()
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

        return new LViewerHostFacts(
            IsVisible,
            pViewerFlyleafHost.IsVisible,
            pSurface?.Visibility.ToString(),
            pSurface?.Width,
            pSurface?.Height,
            pHandle,
            pDisposed,
            pOverlay?.AllowsTransparency);
    }

    private void PViewerAskHandle(LViewerAsk lAsk, Action<bool> lAnswer) =>
        pViewerAsks[lAsk.LViewerAskKind](this, lAsk, lAnswer);

    private static void PViewerLocateHandle(LViewerAsk lAsk, Action<string?> lAnswer)
    {
        var pDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = lAsk.LViewerAskTitle,
            FileName = lAsk.LViewerAskName,
            Filter = lAsk.LViewerAskFilter
        };
        lAnswer(pViewerLocates[pDialog.ShowDialog().GetValueOrDefault()](pDialog));
    }

    private void PViewerDragAccept(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = PLook.PLookDropEffect[LViewer.LViewerSource.LViewerDropResolve(
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pEvent.AllowedEffects.HasFlag(DragDropEffects.Copy))];
        pEvent.Handled = true;
    }

    private void PViewerDropHandle(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = PLook.PLookDropEffect[LViewer.LViewerSource.LViewerDropHandle(
            pEvent.Data.GetData(DataFormats.FileDrop) as string[],
            pEvent.AllowedEffects.HasFlag(DragDropEffects.Copy))];
        pEvent.Handled = true;
    }

    internal bool PViewerSurfaceMatch(nint pForeground)
    {
        try
        {
            return LViewer.LViewerSurfaceMatch(
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
        LViewer.LViewerMedia.LViewerClose();
        Cadroue.Infrastructure.LRenderer.LRendererEngineChange -= PViewerEngineShow;
        Cadroue.Infrastructure.LRenderer.LRendererEngineChange -= PViewerEngineHandle;
        pViewerClockTimer.Tick -= PViewerClockHandle;
        pViewerOverlay.PCropClose();
        pViewerOverlay.DragEnter -= PViewerDragAccept;
        pViewerOverlay.DragOver -= PViewerDragAccept;
        pViewerOverlay.Drop -= PViewerDropHandle;
        PViewerStageRun("flyleaf host", PViewerFlyleafDispose);
        PViewerStageRun("mpv host", PViewerMpvDispose);
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

    private static void PViewerStageRun(string pStage, Action pAction)
    {
        try
        {
            pAction();
        }
        catch (Exception pException)
        {
            Cadroue.Infrastructure.LTraceLog.LTraceErrorRecord($"Viewer close stage '{pStage}' failed", pException);
        }
    }
}
