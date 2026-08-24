using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private const int PViewerOwnerIndex = -8;
    private const uint PViewerPositionFlags = 0x0001 | 0x0002 | 0x0010;
    private static readonly nint pViewerNotTopmost = new(-2);

    private PViewerMpvHost? pViewerMpvHost;
    private Popup? pViewerMpvOverlay;
    private Window? pViewerMpvWindow;
    private bool pViewerMpvActive;
    private bool pViewerEngineSubscribed;
    private string pViewerMpvFilter = string.Empty;
    private string pViewerAudioFilter = string.Empty;
    private string? pViewerAudioApplied;
    private bool pViewerBypass;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int PViewerWindowLongSet(nint pWindow, int pIndex, int pValue);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern nint PViewerWindowLongPtrSet(nint pWindow, int pIndex, nint pValue);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint pWindow,
        nint pInsertAfter,
        int pX,
        int pY,
        int pWidth,
        int pHeight,
        uint pFlags);

    public event Action<bool>? PViewerBypassChange;

    public void PViewerAudioSet(string pViewerGraph)
    {
        pViewerAudioFilter = pViewerGraph ?? string.Empty;
        PViewerAudioApply();
    }

    public bool PViewerBypassRead() => pViewerBypass;

    public void PViewerBypassSet(bool pBypass)
    {
        if (pViewerBypass == pBypass)
        {
            return;
        }

        pViewerBypass = pBypass;
        PViewerAudioUpdate();
        PViewerAudioApply();
        PViewerBypassChange?.Invoke(pViewerBypass);
    }

    private string PViewerAudioResolve() =>
        pViewerBypass ? string.Empty : pViewerAudioFilter;

    private void PViewerAudioToggle() => PViewerBypassSet(!pViewerBypass);

    private void PViewerAudioApply()
    {
        if (!pViewerMpvActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        string pViewerEffective = PViewerAudioResolve();
        if (pViewerEffective == pViewerAudioApplied)
        {
            return;
        }

        try
        {
            pViewerPlayer.PPlayerAudioSet(pViewerEffective);
            pViewerAudioApplied = pViewerEffective;
        }
        catch (Exception pViewerAudioException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected audio filter '{pViewerEffective}': {pViewerAudioException.Message}");
        }
    }

    private Button PViewerAudioBuild()
    {
        var pButton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 52, 0, 0),
            MinWidth = 84,
            Height = 24,
            Padding = new Thickness(12, 0, 12, 0),
            FontSize = 11,
            Visibility = Visibility.Collapsed,
            Style = Cadroue.UIShell.PMainWindow.PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PViewerAudioToggle();
        return pButton;
    }

    private void PViewerAudioUpdate()
    {
        bool pViewerAudioCapable = PViewerEngineCurrent == LPreviewEngine.LPreviewEngineMpv;
        pViewerAudioSwitch.IsEnabled = pViewerAudioCapable;
        pViewerAudioSwitch.Content = LLocalization.LLocalizationTextRead(
            pViewerBypass ? "Viewer.Audio.Original" : "Viewer.Audio.Filtered");
        pViewerAudioSwitch.ToolTip = LLocalization.LLocalizationTextRead(
            pViewerAudioCapable ? "Viewer.Audio.SwitchTooltip" : "Viewer.Audio.MpvRequired");
    }

    private void PViewerAudioShow(bool pViewerAudioVisible)
    {
        pViewerAudioSwitch.Visibility = pViewerAudioVisible && PViewerAudioEligible
            ? Visibility.Visible
            : Visibility.Collapsed;
        if (pViewerAudioVisible && PViewerAudioEligible)
        {
            PViewerAudioUpdate();
        }
    }

    private void PViewerEngineSet(LPreviewEngine pViewerEngine)
    {
        if (PViewerEngineCurrent == pViewerEngine)
        {
            return;
        }

        PViewerEngineCurrent = pViewerEngine;
        PViewerAudioUpdate();
        PViewerEngineChange?.Invoke();
    }

    private void PViewerMpvUpdate()
    {
        if (!pViewerMpvActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        LPreviewState pViewerRender = PViewerRenderRead();
        string pViewerFilter = LPreview.LPreviewFilterResolve(pViewerRender);
        bool pViewerChanged = pViewerFilter != pViewerMpvFilter;
        if (!PViewerFilterSet(pViewerFilter) || !pViewerChanged)
        {
            return;
        }

        if (!LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            try
            {
                pViewerPlayer.PPlayerMpvUpdate();
            }
            catch (Exception pViewerRefreshException)
            {
                LTraceLog.LTraceErrorRecord(
                    $"mpv rejected paused preview refresh: {pViewerRefreshException.Message}");
            }
        }
    }

    private bool PViewerFilterSet(string pViewerFilter)
    {
        if (pViewerFilter == pViewerMpvFilter)
        {
            return true;
        }

        try
        {
            pViewerPlayer.PPlayerFilterSet(pViewerFilter);
            pViewerMpvFilter = pViewerFilter;
            return true;
        }
        catch (Exception pViewerFilterException)
        {
            LTraceLog.LTraceErrorRecord(
                "mpv rejected the preview filter (likely an LGPL libmpv without the GPL eq filter); "
                + "the queued export is unaffected. "
                + $"Filter '{pViewerFilter}': {pViewerFilterException.Message}");
            PViewerFilterClear();
            return false;
        }
    }

    private void PViewerFilterClear()
    {
        try
        {
            pViewerPlayer.PPlayerFilterSet(string.Empty);
            pViewerMpvFilter = string.Empty;
        }
        catch (Exception pViewerClearException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected stale preview filter cleanup: {pViewerClearException.Message}");
        }
    }

    public bool PViewerEditEligible { get; set; }

    public bool PViewerAudioEligible { get; set; }

    private bool PViewerMpvEligible => true;

    private LPreviewEngine PViewerEngineRead() =>
        Cadroue.Infrastructure.LRenderer.LRendererEngineRead();

    private bool PViewerEngineSelect()
    {
        if (!pViewerHostBuilt)
        {
            return false;
        }

        bool pViewerWantMpv = PViewerEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (pViewerWantMpv == pViewerMpvActive)
        {
            return false;
        }

        PPlayerStopDispose();
        if (pViewerMpvActive)
        {
            PViewerMpvDispose();
        }
        else
        {
            PViewerFlyleafDispose();
            pViewerFlyleafHost = null;
        }

        pViewerHostBuilt = false;
        PViewerHostBuild();
        return true;
    }

    private void PViewerEngineHandle()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (pViewerUnloaded || !PViewerMpvEligible)
            {
                return;
            }

            if (!pViewerCommandActive)
            {
                return;
            }

            string? pViewerSourcePath = PViewerSourcePath;
            if (!PViewerEngineSelect() || pViewerSourcePath is null)
            {
                return;
            }

            PPlayerVideoLoad(pViewerSourcePath);
        });
    }

    private void PViewerMpvBuild()
    {
        PViewerOverlayDetach();
        var pViewerMpvSurface = new Grid();
        pViewerMpvHost = new PViewerMpvHost { Visibility = Visibility.Collapsed };
        pViewerMpvSurface.Children.Add(pViewerMpvHost);
        pViewerMpvSurface.Children.Add(pViewerEngineSurface);

        var pViewerOverlayHost = new Grid();
        pViewerOverlayHost.Children.Add(pViewerOverlay);
        pViewerOverlayHost.Children.Add(pViewerCloseButton);
        pViewerOverlayHost.Children.Add(pViewerAudioSwitch);
        pViewerOverlayHost.Children.Add(pViewerEngineOverlay);
        pViewerMpvOverlay = new Popup
        {
            Child = pViewerOverlayHost,
            PlacementTarget = pViewerMpvHost,
            Placement = PlacementMode.Relative,
            AllowsTransparency = true,
            StaysOpen = true,
            IsOpen = false
        };
        pViewerMpvHost.SizeChanged += PViewerOverlayHandle;
        pViewerMpvHost.IsVisibleChanged += PViewerVisibleHandle;

        pViewerSurface = new Border
        {
            Margin = PPanelOuterMargin,
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(0),
            Child = pViewerMpvSurface,
            AllowDrop = true,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };

        Content = pViewerSurface;
        pViewerMpvActive = true;
        pViewerMpvFilter = string.Empty;
        PViewerEngineSet(LPreviewEngine.LPreviewEngineMpv);
    }

    private void PViewerOverlayHandle(object? sender, EventArgs eventArgs) => PViewerOverlayPlace();

    private void PViewerVisibleHandle(object sender, DependencyPropertyChangedEventArgs eventArgs) =>
        PViewerOverlayPlace();

    private void PViewerOverlayPlace()
    {
        if (pViewerMpvOverlay is null || pViewerMpvHost is null)
        {
            return;
        }

        PViewerWindowAttach();

        bool pViewerOverlayShow = pViewerMpvHost.Visibility == Visibility.Visible
            && pViewerMpvHost.IsVisible
            && pViewerMpvHost.ActualWidth > 0
            && pViewerMpvHost.ActualHeight > 0;

        if (!pViewerOverlayShow)
        {
            if (pViewerMpvOverlay.IsOpen)
            {
                LTraceLog.LTraceInfoRecord(
                    $"mpv overlay closed (host {(pViewerMpvHost.IsVisible ? "visible" : "hidden")}, "
                    + $"vis={pViewerMpvHost.Visibility}, size {pViewerMpvHost.ActualWidth:0}x{pViewerMpvHost.ActualHeight:0})");
            }

            pViewerMpvOverlay.IsOpen = false;
            return;
        }

        if (pViewerMpvOverlay.Child is FrameworkElement pViewerOverlayChild)
        {
            pViewerOverlayChild.Width = pViewerMpvHost.ActualWidth;
            pViewerOverlayChild.Height = pViewerMpvHost.ActualHeight;
        }

        if (!pViewerMpvOverlay.IsOpen)
        {
            LTraceLog.LTraceInfoRecord(
                "mpv overlay opened",
                $"top-level transparent window over {pViewerMpvHost.ActualWidth:0}x{pViewerMpvHost.ActualHeight:0}");
        }

        pViewerMpvOverlay.IsOpen = true;
        double pViewerOverlayOffset = pViewerMpvOverlay.HorizontalOffset;
        pViewerMpvOverlay.HorizontalOffset = pViewerOverlayOffset + 0.5;
        pViewerMpvOverlay.HorizontalOffset = pViewerOverlayOffset;
        PViewerOrderApply();
    }

    private void PViewerOrderApply()
    {
        if (pViewerMpvOverlay?.Child is not Visual pViewerOverlayChild
            || PresentationSource.FromVisual(pViewerOverlayChild) is not HwndSource pViewerOverlaySource)
        {
            return;
        }

        nint pViewerOwnerHandle = PViewerWindowHandle(pViewerMpvWindow);
        if (pViewerOwnerHandle == nint.Zero)
        {
            return;
        }

        if (Environment.Is64BitProcess)
        {
            _ = PViewerWindowLongPtrSet(
                pViewerOverlaySource.Handle,
                PViewerOwnerIndex,
                pViewerOwnerHandle);
        }
        else
        {
            _ = PViewerWindowLongSet(
                pViewerOverlaySource.Handle,
                PViewerOwnerIndex,
                pViewerOwnerHandle.ToInt32());
        }

        _ = SetWindowPos(
            pViewerOverlaySource.Handle,
            pViewerNotTopmost,
            0,
            0,
            0,
            0,
            PViewerPositionFlags);
    }

    private void PViewerWindowAttach()
    {
        Window? pViewerMpvHostWindow = Window.GetWindow(this);
        if (ReferenceEquals(pViewerMpvHostWindow, pViewerMpvWindow))
        {
            return;
        }

        PViewerWindowDetach();
        pViewerMpvWindow = pViewerMpvHostWindow;
        if (pViewerMpvWindow is null)
        {
            return;
        }

        pViewerMpvWindow.LocationChanged += PViewerOverlayHandle;
        pViewerMpvWindow.SizeChanged += PViewerOverlayHandle;
    }

    private void PViewerWindowDetach()
    {
        if (pViewerMpvWindow is null)
        {
            return;
        }

        pViewerMpvWindow.LocationChanged -= PViewerOverlayHandle;
        pViewerMpvWindow.SizeChanged -= PViewerOverlayHandle;
        pViewerMpvWindow = null;
    }

    private void PViewerOverlayDetach()
    {
        (pViewerOverlay.Parent as Panel)?.Children.Remove(pViewerOverlay);
        (pViewerCloseButton.Parent as Panel)?.Children.Remove(pViewerCloseButton);
        (pViewerAudioSwitch.Parent as Panel)?.Children.Remove(pViewerAudioSwitch);
        PViewerEngineDetach();
    }

    private async void PViewerMpvApply(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial)
    {
        if (mediaInfo is { LMediaAudioOnly: true } && !pViewerAudioAllowed)
        {
            string pViewerAudioError = LLocalization.LLocalizationTextRead("Viewer.Error.AudioOnlyTab");
            PViewerMpvCommit(new LCargo(sourcePath, null, false, false, pViewerAudioError, pViewerAudioError));
            return;
        }

        string? pViewerPreviewError = null;
        try
        {
            if (!pViewerPlayer.PPlayerReady)
            {
                nint pViewerHandle = nint.Zero;
                if (pViewerMpvHost is not null)
                {
                    if (pViewerMpvHost.PViewerMpvHwnd == nint.Zero)
                    {
                        pViewerMpvHost.Visibility = Visibility.Visible;
                        pViewerMpvHost.UpdateLayout();
                    }

                    pViewerHandle = pViewerMpvHost.PViewerMpvHwnd;
                }

                pViewerPlayer.PPlayerMpvSet(pViewerHandle);
                pViewerMpvFilter = string.Empty;
                pViewerAudioApplied = null;
            }

            pViewerPlayer.PPlayerVolumeSet(pViewerVolume);
            if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
            {
                return;
            }

            if (PCropPersistent)
            {
                PViewerFilterSet(LPreview.LPreviewFilterResolve(PViewerRenderRead()));
            }

            pViewerPlayer.PPlayerMpvCancel();
            await System.Threading.Tasks.Task.Run(() => pViewerPlayer.PPlayerOpen(sourcePath));
        }
        catch (Exception pViewerException)
        {
            pViewerPreviewError = pViewerException.Message;
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            return;
        }

        if (pViewerPreviewError is not null)
        {
            pViewerPlayer.PPlayerDispose();
            PViewerMpvRebuild(sourcePath, mediaInfo, ffmpegError, loadSerial, pViewerPreviewError);
            return;
        }

        bool pViewerPreviewOk = pViewerPlayer.PPlayerReady && pViewerPreviewError is null;
        PViewerMpvCommit(new LCargo(
            sourcePath,
            mediaInfo,
            mediaInfo is not null,
            pViewerPreviewOk,
            ffmpegError,
            pViewerPreviewError));
    }

    private void PViewerMpvCommit(LCargo pViewerStatus)
    {
        PViewerNeutralCancel();
        bool pViewerHasPreview = pViewerPlayer.PPlayerReady && pViewerStatus.LCargoPreviewAvailable;
        string pViewerPath = pViewerStatus.LCargoSourcePath ?? "(no path)";
        string pViewerFileName = System.IO.Path.GetFileName(pViewerPath);

        if (pViewerStatus.LCargoMediaInfo is { } pViewerInfo)
        {
            LTraceLog.LTraceInfoRecord(
                $"Media opened '{pViewerFileName}': {pViewerInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff} (mpv preview) [{pViewerPath}]");
        }
        else
        {
            LTraceLog.LTraceErrorRecord($"Media rejected '{pViewerFileName}': {pViewerStatus.LCargoFfmpegError ?? "unreadable"} [{pViewerPath}]");
        }

        pPlayerAccurateActive = false;
        PViewerHostShow(pViewerHasPreview);
        pViewerMediaInfo = pViewerStatus.LCargoMediaInfo;
        PViewerSourcePath = pViewerStatus.LCargoSourcePath;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        if (!PCropPersistent)
        {
            PCropVideo = null;
            LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(null);
            PCropHide();
        }

        PViewerMediaRaise(pViewerStatus);
        if (!pViewerHasPreview)
        {
            pViewerPlayer.PPlayerDispose();
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        PViewerMpvUpdate();
        PViewerAudioApply();

        if (LPreference.LPreferenceStateCurrent.LPreferenceAutoplay)
        {
            pViewerResumeInactive = false;
            pViewerPlayer.PPlayerPlay();
            PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
            pViewerClockTimer.Start();
        }
        else
        {
            pViewerPlayer.PPlayerPause();
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
        }
    }

    private void PViewerMpvRebuild(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial, string mpvReason)
    {
        string pViewerRebuildName = System.IO.Path.GetFileName(sourcePath);

        if (PViewerEditEligible)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv could not open '{pViewerRebuildName}': {mpvReason}; preview unavailable (mpv is the Edit preview engine, no Flyleaf fallback) [{sourcePath}]");
            PViewerMpvCommit(new LCargo(
                sourcePath, mediaInfo, mediaInfo is not null, false, ffmpegError, mpvReason));
            return;
        }

        LTraceLog.LTraceErrorRecord(
            $"mpv could not open '{pViewerRebuildName}': {mpvReason}; falling back to the existing engine for this file [{sourcePath}]");

        PViewerMpvDispose();
        pViewerHostBuilt = false;
        PViewerFlyleafBuild();
        pViewerHostBuilt = true;

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            return;
        }

        PPlayerMediaApply(sourcePath, mediaInfo, ffmpegError, loadSerial);
    }

    private void PViewerMpvDispose()
    {
        PViewerWindowDetach();

        if (pViewerMpvOverlay is not null)
        {
            pViewerMpvOverlay.IsOpen = false;
            pViewerMpvOverlay.Child = null;
            pViewerMpvOverlay = null;
        }

        PViewerOverlayDetach();

        if (pViewerMpvHost is null)
        {
            pViewerMpvActive = false;
            pViewerMpvFilter = string.Empty;
            pViewerAudioApplied = null;
            return;
        }

        pViewerMpvHost.SizeChanged -= PViewerOverlayHandle;
        pViewerMpvHost.IsVisibleChanged -= PViewerVisibleHandle;

        try
        {
            ((IDisposable)pViewerMpvHost).Dispose();
        }
        catch
        {
        }

        pViewerMpvHost = null;
        pViewerMpvActive = false;
        pViewerMpvFilter = string.Empty;
        pViewerAudioApplied = null;
    }
}
