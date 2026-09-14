using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PViewer
{
    private PViewerMpvHost? pViewerMpvHost;
    private bool pViewerMpvActive;
    private bool pViewerEngineSubscribed;
    private string pViewerMpvFilter = string.Empty;

    public bool PViewerEditEligible { get; set; }

    private bool PViewerMpvEligible => true;

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
            if (pViewerUnloaded || !PViewerMpvEligible || !pViewerCommandActive)
            {
                return;
            }

            if (pViewerMpvActive == (PViewerEngineRead() == LPreviewEngine.LPreviewEngineMpv))
            {
                return;
            }

            PViewerEngineRestore();
        });
    }

    private bool PViewerEngineRestore()
    {
        PViewerIntent? pViewerPending = pViewerIntent;
        string? pViewerSourcePath = PViewerSourcePath;
        bool pViewerPlaying = pViewerResumeInactive || LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;
        TimeSpan pViewerPosition = pViewerPlayer.PPlayerReady
            ? pViewerPlayer.PPlayerTimeRead()
            : LPreviewStateCurrent.LPlaybackState.LPlaybackPosition;
        bool pViewerSwapped = PViewerEngineSelect();
        if (pViewerPending is { } pViewerRequest)
        {
            PPlayerVideoLoad(pViewerRequest);
            return true;
        }

        if (!pViewerSwapped || pViewerSourcePath is null)
        {
            return false;
        }

        PPlayerVideoLoad(new PViewerIntent(pViewerSourcePath, pViewerPosition, pViewerPlaying));
        return true;
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
        pViewerOverlayHost.Children.Add(pViewerPreviewButton);
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

            await System.Threading.Tasks.Task.Run(() => pViewerPlayer.PPlayerOpen(sourcePath));
        }
        catch (Exception pViewerException)
        {
            pViewerPreviewError = pViewerException.Message;
        }

        if (loadSerial != pViewerLoadSerial || pViewerUnloaded || !pViewerCommandActive)
        {
            PViewerMpvRestore();
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

    private void PViewerMpvRestore()
    {
        if (pViewerUnloaded || !pViewerMpvActive || pViewerIntent is not null)
        {
            return;
        }

        if (PViewerSourcePath is { } pViewerKeptPath)
        {
            var pViewerRequest = new PViewerIntent(
                pViewerKeptPath,
                LPreviewStateCurrent.LPlaybackState.LPlaybackPosition,
                false);
            if (pViewerCommandActive)
            {
                PPlayerVideoLoad(pViewerRequest);
            }
            else
            {
                pViewerIntent = pViewerRequest;
            }

            return;
        }

        if (pViewerPlayer.PPlayerReady)
        {
            pViewerPlayer.PPlayerStop();
            PViewerHostShow(false);
        }
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
                $"Media opened '{pViewerFileName}': {pViewerInfo.LMediaInfoDuration:hh\\:mm\\:ss\\.fff} "
                + $"(mpv preview) [{pViewerPath}]");
        }
        else
        {
            LTraceLog.LTraceErrorRecord(
                $"Media rejected '{pViewerFileName}': {pViewerStatus.LCargoFfmpegError ?? "unreadable"} "
                + $"[{pViewerPath}]");
        }

        pPlayerAccurateActive = false;
        PViewerHostShow(pViewerHasPreview);
        pViewerMediaInfo = pViewerStatus.LCargoMediaInfo;
        PViewerSourcePath = pViewerStatus.LCargoSourcePath;
        LPreviewStateCurrent = LPreviewStateCurrent.LPlaybackStateChange(LPlaybackState.LPlaybackStoppedCreate());
        pViewerEndReached = false;
        if (!PCropPersistent)
        {
            PCropVideo = null;
            LPreviewStateCurrent = LPreviewStateCurrent.LCropboxChange(null);
            PCropHide();
        }

        PViewerIntent? pViewerRequest = pViewerIntent;
        pViewerIntent = null;
        PViewerMediaRaise(pViewerStatus);
        if (!pViewerHasPreview)
        {
            pViewerPlayer.PPlayerDispose();
            PViewerPlaybackUpdate(false, TimeSpan.Zero);
            return;
        }

        PViewerMpvUpdate();
        PViewerAudioApply();
        PViewerIntentApply(pViewerRequest);
    }

    private void PViewerMpvRebuild(
        string sourcePath,
        LMediaInfo? mediaInfo,
        string? ffmpegError,
        int loadSerial,
        string mpvReason)
    {
        string pViewerRebuildName = System.IO.Path.GetFileName(sourcePath);

        if (PViewerEditEligible)
        {
            LTraceLog.LTraceErrorRecord(
                $"mpv could not open '{pViewerRebuildName}': {mpvReason}; preview unavailable "
                + $"(mpv is the Edit preview engine, no Flyleaf fallback) [{sourcePath}]");
            PViewerMpvCommit(new LCargo(
                sourcePath, mediaInfo, mediaInfo is not null, false, ffmpegError, mpvReason));
            return;
        }

        LTraceLog.LTraceErrorRecord(
            $"mpv could not open '{pViewerRebuildName}': {mpvReason}; "
            + $"falling back to the existing engine for this file [{sourcePath}]");

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
