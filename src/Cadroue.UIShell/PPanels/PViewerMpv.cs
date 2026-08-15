using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private PViewerMpvHost? pViewerMpvHost;
    private Popup? pViewerMpvOverlay;
    private Window? pViewerMpvWindow;
    private bool pViewerMpvActive;
    private bool pViewerEngineSubscribed;
    private string pViewerMpvFilter = string.Empty;
    private LColor? pViewerMpvGammaApplied;

    private void PViewerEngineCurrentSet(LPreviewEngine pViewerEngine)
    {
        if (PViewerEngineCurrent == pViewerEngine)
        {
            return;
        }

        PViewerEngineCurrent = pViewerEngine;
        PViewerEngineChange?.Invoke();
    }

    private void PViewerMpvPreviewApply()
    {
        if (!pViewerMpvActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        LColor pViewerGamma = LPreviewStateCurrent.LColor;
        bool pViewerGammaFiltered = LPreview.LPreviewGammaCheck(pViewerGamma);
        LPreviewMpvEqualizer pViewerEqualizer =
            LPreview.LPreviewEqualizerResolve(LPreviewStateCurrent);
        string pViewerFilter = LPreview.LPreviewFilterResolve(LPreviewStateCurrent);
        bool pViewerApplied;

        if (pViewerGammaFiltered)
        {
            pViewerApplied = PViewerMpvEqualizerApply(pViewerEqualizer, true);
            if (pViewerApplied)
            {
                pViewerApplied = PViewerMpvFilterApply(pViewerFilter, true);
            }
            else
            {
                PViewerMpvRejectedFilterClear();
            }
        }
        else
        {
            // Remove filtered Gamma before restoring the native factor so the two
            // corrections can never be visible on the same frame.
            pViewerApplied = PViewerMpvFilterApply(
                pViewerFilter,
                pViewerMpvGammaApplied is { } pViewerPreviousGamma
                && LPreview.LPreviewGammaCheck(pViewerPreviousGamma));
            if (pViewerApplied)
            {
                pViewerApplied = PViewerMpvEqualizerApply(pViewerEqualizer, false);
            }
        }

        if (pViewerApplied && pViewerMpvGammaApplied != pViewerGamma)
        {
            pViewerMpvGammaApplied = pViewerGamma;
            if (!LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
            {
                try
                {
                    pViewerPlayer.PPlayerMpvRefresh();
                }
                catch (Exception pViewerRefreshException)
                {
                    LTraceLog.LTraceErrorRecord(
                        $"mpv rejected paused preview refresh: {pViewerRefreshException.Message}");
                }
            }
        }
    }

    private bool PViewerMpvEqualizerApply(
        LPreviewMpvEqualizer pViewerEqualizer,
        bool pViewerGammaFirst)
    {
        try
        {
            if (pViewerGammaFirst)
            {
                pViewerPlayer.PPlayerMpvGammaSet(pViewerEqualizer.LPreviewMpvGamma);
            }

            pViewerPlayer.PPlayerEqualizerSet(
                pViewerEqualizer.LPreviewMpvBrightness,
                pViewerEqualizer.LPreviewMpvContrast,
                pViewerEqualizer.LPreviewMpvSaturation,
                pViewerEqualizer.LPreviewMpvHue);
            if (!pViewerGammaFirst)
            {
                pViewerPlayer.PPlayerMpvGammaSet(pViewerEqualizer.LPreviewMpvGamma);
            }

            return true;
        }
        catch (Exception pViewerEqualizerException)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv rejected preview properties: {pViewerEqualizerException.Message}");
            return false;
        }
    }

    private bool PViewerMpvFilterApply(string pViewerFilter, bool pViewerAdvancedGamma)
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
                $"mpv rejected preview filter '{pViewerFilter}': {pViewerFilterException.Message}");
            if (pViewerAdvancedGamma)
            {
                PViewerMpvRejectedFilterClear();
            }

            return false;
        }
    }

    private void PViewerMpvRejectedFilterClear()
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

    private LPreviewEngine PViewerEngineRead() =>
        PViewerEditEligible
            ? Cadroue.Infrastructure.LRenderer.LRendererEngineRead()
            : LPreviewEngine.LPreviewEngineFlyleaf;

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
            if (pViewerUnloaded || !PViewerEditEligible)
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

        var pViewerOverlayHost = new Grid();
        pViewerOverlayHost.Children.Add(pViewerOverlay);
        pViewerOverlayHost.Children.Add(pViewerCloseButton);
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
        pViewerMpvGammaApplied = null;
        PViewerEngineCurrentSet(LPreviewEngine.LPreviewEngineMpv);
    }

    private void PViewerOverlayHandle(object? sender, EventArgs eventArgs) => PViewerOverlayPlace();

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
            && pViewerMpvHost.ActualHeight > 0
            && (pViewerMpvWindow?.IsActive ?? true);

        if (!pViewerOverlayShow)
        {
            pViewerMpvOverlay.IsOpen = false;
            return;
        }

        if (pViewerMpvOverlay.Child is FrameworkElement pViewerOverlayChild)
        {
            pViewerOverlayChild.Width = pViewerMpvHost.ActualWidth;
            pViewerOverlayChild.Height = pViewerMpvHost.ActualHeight;
        }

        pViewerMpvOverlay.IsOpen = true;
        double pViewerOverlayOffset = pViewerMpvOverlay.HorizontalOffset;
        pViewerMpvOverlay.HorizontalOffset = pViewerOverlayOffset + 0.5;
        pViewerMpvOverlay.HorizontalOffset = pViewerOverlayOffset;
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
        pViewerMpvWindow.Activated += PViewerOverlayHandle;
        pViewerMpvWindow.Deactivated += PViewerOverlayHandle;
    }

    private void PViewerWindowDetach()
    {
        if (pViewerMpvWindow is null)
        {
            return;
        }

        pViewerMpvWindow.LocationChanged -= PViewerOverlayHandle;
        pViewerMpvWindow.SizeChanged -= PViewerOverlayHandle;
        pViewerMpvWindow.Activated -= PViewerOverlayHandle;
        pViewerMpvWindow.Deactivated -= PViewerOverlayHandle;
        pViewerMpvWindow = null;
    }

    private void PViewerOverlayDetach()
    {
        (pViewerOverlay.Parent as Panel)?.Children.Remove(pViewerOverlay);
        (pViewerCloseButton.Parent as Panel)?.Children.Remove(pViewerCloseButton);
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
                    if (pViewerMpvHost.PViewerMpvHandle == nint.Zero)
                    {
                        pViewerMpvHost.Visibility = Visibility.Visible;
                        pViewerMpvHost.UpdateLayout();
                    }

                    pViewerHandle = pViewerMpvHost.PViewerMpvHandle;
                }

                pViewerPlayer.PPlayerMpvSet(pViewerHandle);
            }

            pViewerPlayer.PPlayerVolumeSet(pViewerVolume);
            await System.Threading.Tasks.Task.Run(() => pViewerPlayer.PPlayerOpen(sourcePath));
        }
        catch (Exception pViewerException)
        {
            pViewerPreviewError = pViewerException.Message;
            pViewerPlayer.PPlayerDispose();
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            return;
        }

        if (pViewerPreviewError is not null)
        {
            PViewerMpvFallback(sourcePath, mediaInfo, ffmpegError, loadSerial, pViewerPreviewError);
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

        PViewerMpvPreviewApply();

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

    private void PViewerMpvFallback(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial, string mpvReason)
    {
        string pViewerFallbackName = System.IO.Path.GetFileName(sourcePath);
        LTraceLog.LTraceErrorRecord(
            $"mpv could not open '{pViewerFallbackName}': {mpvReason}; falling back to the existing engine for this file [{sourcePath}]");

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
            pViewerMpvGammaApplied = null;
            return;
        }

        pViewerMpvHost.SizeChanged -= PViewerOverlayHandle;

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
        pViewerMpvGammaApplied = null;
    }
}
