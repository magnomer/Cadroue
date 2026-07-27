using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIShell;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer : PPanel
{
    private readonly Border pViewerSurface;
    private readonly FlyleafHost pViewerFlyleafHost;
    private readonly Canvas pViewerOverlay;
    private readonly Rectangle pViewerCropBox;
    private readonly DispatcherTimer pViewerClockTimer;
    private Player? pViewerPlayer;
    private LMediaInfo? pViewerMediaInfo;
    private Point? pViewerCropStartPoint;
    private int pViewerLoadSerial;
    private double pViewerVolume = App.LPreferenceStateCurrent.LPreferenceVolume;
    private bool pViewerAudioOnlyAllowed;
    private bool pViewerCommandActive;
    private bool pViewerResumeAfterInactive;
    private bool pViewerUnloaded;

    public event Action<LMediaOpenStatus>? PViewerMediaChange;
    public event Action<TimeSpan>? PViewerClockTick;

    public string? PViewerSourcePath { get; private set; }
    public Rect? PCropVideo { get; private set; }
    public double PViewerVolumeCurrent => pViewerVolume;
    public LPreviewState LPreviewStateCurrent { get; private set; } = LPreviewState.LPreviewDefaultCreate();

    public PViewer() : base("")
    {
        AllowDrop = true;
        Focusable = true;
        FocusVisualStyle = null;

        pViewerCropBox = new Rectangle
        {
            Stroke = Brushes.White,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = Brushes.Transparent,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };

        pViewerOverlay = new Canvas
        {
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
            Focusable = true,
            AllowDrop = true
        };
        pViewerOverlay.Children.Add(pViewerCropBox);
        pViewerOverlay.MouseLeftButtonDown += PCropPressHandle;
        pViewerOverlay.MouseMove += PCropMoveHandle;
        pViewerOverlay.MouseLeftButtonUp += PCropReleaseHandle;
        pViewerOverlay.SizeChanged += PCropSizeHandle;

        pViewerFlyleafHost = new FlyleafHost
        {
            Content = pViewerOverlay,
            VideoBackground = Brushes.White,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None
        };

        pViewerSurface = new Border
        {
            Margin = PPanelOuterMargin,
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(0),
            Child = pViewerFlyleafHost,
            AllowDrop = true,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };
        Content = pViewerSurface;

        PDropHandlersAdd();

        pViewerClockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        pViewerClockTimer.Tick += PViewerClockHandle;
    }

    public void PViewerPlay()
    {
        if (!pViewerCommandActive || pViewerPlayer is null)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            return;
        }

        pViewerResumeAfterInactive = false;
        pViewerPlayer.Play();
        PViewerPlaybackUpdate(true, PPlayerTimeRead(pViewerPlayer));
        pViewerClockTimer.Start();
    }

    public void PViewerPause()
    {
        if (!pViewerCommandActive || pViewerPlayer is null)
        {
            return;
        }

        pViewerResumeAfterInactive = false;
        pViewerPlayer.Pause();
        PViewerPlaybackUpdate(false, PPlayerTimeRead(pViewerPlayer));
        pViewerClockTimer.Stop();
    }

    public void PViewerSeek(TimeSpan playbackPosition)
    {
        if (!pViewerCommandActive || pViewerPlayer is null)
        {
            return;
        }

        pViewerPlayer.Seek((int)playbackPosition.TotalMilliseconds);
        PViewerPlaybackUpdate(null, playbackPosition);
    }

    public void PViewerVolumeSet(double volume)
    {
        if (!pViewerCommandActive) return;
        pViewerVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
        if (App.LPreferenceStateCurrent.LPreferenceVolumeSingleGlobal)
            App.LPreferenceVolumeSet(pViewerVolume);
        if (pViewerPlayer is null)
        {
            return;
        }

        pViewerPlayer.Audio.Volume = (int)Math.Round(pViewerVolume);
    }

    public void PViewerAudioSet(bool pAudioOnlyAllowed)
    {
        pViewerAudioOnlyAllowed = pAudioOnlyAllowed;
    }

    public void PViewerCommandSet(bool pCommandActive)
    {
        if (pViewerCommandActive == pCommandActive)
        {
            return;
        }

        if (!pCommandActive)
        {
            PPlayerSuspend();
            pViewerCommandActive = false;
            pViewerLoadSerial++;
            pViewerClockTimer.Stop();
            return;
        }

        pViewerCommandActive = true;
        PPlayerResume();
    }

    public void PViewerSourceOpen(string sourcePath)
    {
        if (!pViewerCommandActive || string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        if (LSidecarStore.LSidecarFileCheck(sourcePath))
        {
            if (PViewerSidecarResolve(sourcePath) is not { } pResolvedPath)
            {
                return;
            }

            sourcePath = pResolvedPath;
        }

        _ = PPlayerVideoLoad(sourcePath);
    }

    private string? PViewerSidecarResolve(string pSidecarPath)
    {
        LSidecar? pSidecar = LSidecarStore.LSidecarRead(pSidecarPath);
        if (pSidecar is null)
        {
            MessageBox.Show(
                "That .cad file could not be read.",
                "Open",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        LSidecarSourceResult pResult = LSidecarSource.LSidecarSourceResolve(pSidecarPath, pSidecar);
        if (pResult.LSidecarResultVerified)
        {
            return pResult.LSidecarResultPath;
        }

        if (pResult.LSidecarResultKind != LSidecarSourceKind.LSidecarSourceMissing
            && MessageBox.Show(
                $"The media for this .cad was found at:\n\n{pResult.LSidecarResultPath}\n\n"
                + "but it does not match what the .cad recorded. Open it anyway?",
                "Open",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            return pResult.LSidecarResultPath;
        }

        return PViewerSidecarLocate(pSidecar);
    }

    private string? PViewerSidecarLocate(LSidecar pSidecar)
    {
        var pDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = $"Locate media for this .cad ({pSidecar.Source.FileName})",
            FileName = pSidecar.Source.FileName,
            Filter = "Media files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.ts;*.mts;*.m2ts|All files|*.*"
        };

        if (pDialog.ShowDialog() != true)
        {
            return null;
        }

        if (LSidecarSource.LSidecarVerifyCheck(pDialog.FileName, pSidecar.Source))
        {
            return pDialog.FileName;
        }

        return MessageBox.Show(
            "That file does not match what the .cad recorded. Open it anyway?",
            "Open",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes
            ? pDialog.FileName
            : null;
    }

    private void PViewerPreviewApply()
    {
        LPreview.LPreviewApply(pViewerPlayer, LPreviewStateCurrent);
    }
}

public enum LMediaOpenStatusKind
{
    LMediaOpenStatusProcessablePreviewAvailable,
    LMediaOpenStatusProcessablePreviewUnavailable,
    LMediaOpenStatusUnprocessablePreviewAvailable,
    LMediaOpenStatusUnprocessablePreviewUnavailable
}

public sealed record LMediaOpenStatus(
    string LMediaOpenSourcePath,
    LMediaInfo? LMediaOpenMediaInfo,
    bool LMediaOpenFfmpegProcessable,
    bool LMediaOpenPreviewAvailable,
    string? LMediaOpenFfmpegError,
    string? LMediaOpenPreviewError)
{
    public LMediaOpenStatusKind LMediaOpenStatusKind =>
        LMediaOpenFfmpegProcessable && LMediaOpenPreviewAvailable
            ? LMediaOpenStatusKind.LMediaOpenStatusProcessablePreviewAvailable
            : LMediaOpenFfmpegProcessable
                ? LMediaOpenStatusKind.LMediaOpenStatusProcessablePreviewUnavailable
                : LMediaOpenPreviewAvailable
                    ? LMediaOpenStatusKind.LMediaOpenStatusUnprocessablePreviewAvailable
                    : LMediaOpenStatusKind.LMediaOpenStatusUnprocessablePreviewUnavailable;

    public string LMediaOpenStatusText => LMediaOpenStatusKind switch
    {
        LMediaOpenStatusKind.LMediaOpenStatusProcessablePreviewAvailable => "FFmpeg processable / preview available",
        LMediaOpenStatusKind.LMediaOpenStatusProcessablePreviewUnavailable => "FFmpeg processable / preview unavailable",
        LMediaOpenStatusKind.LMediaOpenStatusUnprocessablePreviewAvailable => "FFmpeg unprocessable / preview available",
        _ => "FFmpeg unprocessable / preview unavailable"
    };
}
