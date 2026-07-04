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

public sealed partial class PViewerPanel : PPanelFrame
{
    private readonly Border pViewerPanelSurface;
    private readonly FlyleafHost pViewerPanelFlyleafHost;
    private readonly Canvas pViewerPanelOverlay;
    private readonly Rectangle pViewerPanelCropBox;
    private readonly DispatcherTimer pViewerPanelClockTimer;
    private Player? pViewerPanelPlayer;
    private LMediaInfo? pViewerPanelMediaInfo;
    private Point? pViewerPanelCropStartPoint;
    private int pViewerPanelLoadSerial;
    private double pViewerPanelVolume = App.LPreferenceStateCurrent.LPreferenceVolume;
    private bool pViewerPanelAudioOnlyAllowed;
    private bool pViewerPanelCommandActive;
    private bool pViewerPanelResumeAfterInactive;
    private bool pViewerPanelUnloaded;

    public event Action<LMediaOpenStatus>? PViewerPanelMediaStatusChange;
    public event Action<TimeSpan>? PViewerPanelClockTick;

    public string? PViewerPanelSourcePathCurrent { get; private set; }
    public Rect? PViewerPanelCropBoxVideo { get; private set; }
    public double PViewerPanelVolumeCurrent => pViewerPanelVolume;
    public LPreviewState LPreviewStateCurrent { get; private set; } = LPreviewState.LPreviewStateDefaultCreate();

    public PViewerPanel() : base("")
    {
        AllowDrop = true;
        Focusable = true;
        FocusVisualStyle = null;

        pViewerPanelCropBox = new Rectangle
        {
            Stroke = Brushes.White,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 4, 3 },
            Fill = Brushes.Transparent,
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false
        };

        pViewerPanelOverlay = new Canvas
        {
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
            Focusable = true,
            AllowDrop = true
        };
        pViewerPanelOverlay.Children.Add(pViewerPanelCropBox);
        pViewerPanelOverlay.MouseLeftButtonDown += PViewerPanelCropMouseDown;
        pViewerPanelOverlay.MouseMove += PViewerPanelCropMouseMove;
        pViewerPanelOverlay.MouseLeftButtonUp += PViewerPanelCropMouseUp;
        pViewerPanelOverlay.SizeChanged += PViewerPanelOverlaySizeChanged;

        pViewerPanelFlyleafHost = new FlyleafHost
        {
            Content = pViewerPanelOverlay,
            VideoBackground = Brushes.White,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None
        };

        pViewerPanelSurface = new Border
        {
            Margin = PPanelOuterMargin,
            BorderBrush = PPanelLineBrush,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(0),
            Child = pViewerPanelFlyleafHost,
            AllowDrop = true,
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };
        Content = pViewerPanelSurface;

        PViewerPanelDropHandlersAdd();

        pViewerPanelClockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        pViewerPanelClockTimer.Tick += PViewerPanelClockTickHandle;
    }

    public void PViewerPanelPlayRequest()
    {
        if (!pViewerPanelCommandActive || pViewerPanelPlayer is null)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            return;
        }

        pViewerPanelResumeAfterInactive = false;
        pViewerPanelPlayer.Play();
        PViewerPanelPlaybackStateUpdate(true, PViewerPanelPlayerTimeRead(pViewerPanelPlayer));
        pViewerPanelClockTimer.Start();
    }

    public void PViewerPanelPauseRequest()
    {
        if (!pViewerPanelCommandActive || pViewerPanelPlayer is null)
        {
            return;
        }

        pViewerPanelResumeAfterInactive = false;
        pViewerPanelPlayer.Pause();
        PViewerPanelPlaybackStateUpdate(false, PViewerPanelPlayerTimeRead(pViewerPanelPlayer));
        pViewerPanelClockTimer.Stop();
    }

    public void PViewerPanelSeekRequest(TimeSpan playbackPosition)
    {
        if (!pViewerPanelCommandActive || pViewerPanelPlayer is null)
        {
            return;
        }

        pViewerPanelPlayer.Seek((int)playbackPosition.TotalMilliseconds);
        PViewerPanelPlaybackStateUpdate(null, playbackPosition);
    }

    public void PViewerPanelVolumeRequest(double volume)
    {
        if (!pViewerPanelCommandActive) return;
        pViewerPanelVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
        if (App.LPreferenceStateCurrent.LPreferenceVolumeSingleGlobal)
            App.LPreferenceVolumeSet(pViewerPanelVolume);
        if (pViewerPanelPlayer is null)
        {
            return;
        }

        pViewerPanelPlayer.Audio.Volume = (int)Math.Round(pViewerPanelVolume);
    }

    public void PViewerPanelAudioOnlyAllowSet(bool pAudioOnlyAllowed)
    {
        pViewerPanelAudioOnlyAllowed = pAudioOnlyAllowed;
    }

    public void PViewerPanelCommandActiveSet(bool pCommandActive)
    {
        if (pViewerPanelCommandActive == pCommandActive)
        {
            return;
        }

        if (!pCommandActive)
        {
            PViewerPanelPlaybackSuspendForInactive();
            pViewerPanelCommandActive = false;
            pViewerPanelLoadSerial++;
            pViewerPanelClockTimer.Stop();
            return;
        }

        pViewerPanelCommandActive = true;
        PViewerPanelPlaybackResumeForActive();
    }

    public void PViewerPanelSourceOpenRequest(string sourcePath)
    {
        if (!pViewerPanelCommandActive || string.IsNullOrWhiteSpace(sourcePath))
        {
            return;
        }

        _ = PViewerPanelVideoLoadAsynchronous(sourcePath);
    }


    private void PViewerPanelPreviewStateApply()
    {
        LPreviewFlyleafApply.LPreviewApply(pViewerPanelPlayer, LPreviewStateCurrent);
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
