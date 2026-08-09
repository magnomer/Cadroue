using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.Core;
using Cadroue.Media;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer
{
    private PViewerMpvHost? pViewerMpvHost;
    private bool pViewerMpvActive;
    private bool pViewerEngineSubscribed;

    public bool PViewerEditEligible { get; set; }

    private LPreviewEngine PViewerEngineRead() =>
        PViewerEditEligible
            ? Cadroue.Infrastructure.LRenderer.LRendererEngineRead()
            : LPreviewEngine.LPreviewEngineFlyleaf;

    private void PViewerEngineSelect()
    {
        if (!pViewerHostBuilt)
        {
            return;
        }

        bool pViewerWantMpv = PViewerEngineRead() == LPreviewEngine.LPreviewEngineMpv;
        if (pViewerWantMpv == pViewerMpvActive)
        {
            return;
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
    }

    private void PViewerEngineHandle()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (pViewerUnloaded || !PViewerEditEligible)
            {
                return;
            }

            if (PViewerSourcePath is not null || LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
            {
                return;
            }

            PViewerEngineSelect();
        });
    }

    private void PViewerMpvBuild()
    {
        PViewerOverlayDetach();
        var pViewerMpvSurface = new Grid();
        pViewerMpvHost = new PViewerMpvHost { Visibility = Visibility.Collapsed };
        pViewerMpvSurface.Children.Add(pViewerMpvHost);
        pViewerMpvSurface.Children.Add(pViewerOverlay);
        pViewerMpvSurface.Children.Add(pViewerCloseButton);

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
    }

    private void PViewerOverlayDetach()
    {
        (pViewerOverlay.Parent as Panel)?.Children.Remove(pViewerOverlay);
        (pViewerCloseButton.Parent as Panel)?.Children.Remove(pViewerCloseButton);
    }

    private void PViewerMpvApply(string sourcePath, LMediaInfo? mediaInfo, string? ffmpegError, int loadSerial)
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
            pViewerPlayer.PPlayerOpen(sourcePath);
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

    private void PViewerMpvDispose()
    {
        if (pViewerMpvHost is null)
        {
            return;
        }

        try
        {
            ((IDisposable)pViewerMpvHost).Dispose();
        }
        catch
        {
        }

        pViewerMpvHost = null;
        pViewerMpvActive = false;
    }
}
