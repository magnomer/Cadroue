using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PViewer
{
    private PViewerMpvHost? pViewerMpvHost;

    private bool PViewerMpvEligible => true;

    private void PViewerMpvUpdate()
    {
        if (!LViewer.LViewerMpvActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        LPreviewState pViewerRender = PViewerRenderRead();
        string pViewerFilter = LPreview.LPreviewFilterResolve(pViewerRender);
        bool pViewerChanged = pViewerFilter != LPlayer.LPlayerFilterApplied;
        if (!PViewerFilterSet(pViewerFilter) || !pViewerChanged)
        {
            return;
        }

        if (!LViewer.LViewerPlaying)
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
        if (pViewerFilter == LPlayer.LPlayerFilterApplied)
        {
            return true;
        }

        try
        {
            pViewerPlayer.PPlayerFilterSet(pViewerFilter);
            LPlayer.LPlayerFilterSet(pViewerFilter);
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
            LPlayer.LPlayerFilterSet(string.Empty);
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
        LViewer.LViewerMpvSet(true);
        LPlayer.LPlayerFilterSet(string.Empty);
        PViewerEngineSet(LPreviewEngine.LPreviewEngineMpv);
    }

    private async void PViewerMpvApply(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial)
    {
        if (mediaInfo is { LMediaAudioOnly: true } && !LViewer.LViewerAudioAllowed)
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
                LPlayer.LPlayerAppliedReset();
            }

            pViewerPlayer.PPlayerVolumeSet(LViewer.LViewerVolume);
            if (!LViewer.LViewerSerialCheck(loadSerial))
            {
                return;
            }

            if (PCropPersistent)
            {
                PViewerFilterSet(LPreview.LPreviewFilterResolve(PViewerRenderRead()));
            }

            await LPlayer.LPlayerOpenStart(sourcePath, pViewerPlayer.PPlayerOpen);
        }
        catch (Exception pViewerException)
        {
            pViewerPreviewError = pViewerException.Message;
        }

        if (!LViewer.LViewerSerialCheck(loadSerial))
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
        if (LViewer.LViewerUnloaded || !LViewer.LViewerMpvActive || LViewer.LViewerIntent is not null)
        {
            return;
        }

        if (PViewerSourcePath is { } pViewerKeptPath)
        {
            var pViewerRequest = new LViewerIntent(pViewerKeptPath, LViewer.LViewerPosition, false);
            if (LViewer.LViewerCommandActive)
            {
                PPlayerVideoLoad(pViewerRequest);
            }
            else
            {
                LViewer.LViewerIntentSet(pViewerRequest);
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

        LPlayer.LPlayerAccurateReset();
        PViewerHostShow(pViewerHasPreview);
        LViewerIntent? pViewerRequest = PViewerCargoApply(pViewerStatus);
        if (!pViewerHasPreview)
        {
            pViewerPlayer.PPlayerDispose();
            LViewer.LViewerPlaybackUpdate(false, TimeSpan.Zero);
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

        if (LViewer.LViewerEditEligible)
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
        LViewer.LViewerHostSet(false);
        PViewerFlyleafBuild();
        LViewer.LViewerHostSet(true);

        if (!LViewer.LViewerSerialCheck(loadSerial))
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
        LViewer.LViewerMpvSet(false);
        LPlayer.LPlayerAppliedReset();
        if (pViewerMpvHost is null)
        {
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
    }
}
