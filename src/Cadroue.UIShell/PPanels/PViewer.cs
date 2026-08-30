using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIShell;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;
using Cadroue.Application;

using Cadroue.Infrastructure;


namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer : PPanel
{
    private enum PViewerTool
    {
        None,
        Crop,
        Neutral
    }

    private Border? pViewerSurface;
    private readonly Button pViewerCloseButton;
    private readonly Button pViewerPreviewButton;
    private PSLoupe? pViewerLoupe;
    private readonly Button pViewerAudioSwitch;
    private readonly Border pViewerEngineSurface;
    private readonly Border pViewerEngineOverlay;
    private FlyleafHost? pViewerFlyleafHost;
    private bool pViewerHostBuilt;
    private readonly Canvas pViewerOverlay;
    private readonly Rectangle pViewerCropBox;
    private readonly DispatcherTimer pViewerClockTimer;
    private volatile bool pPlayerAccurateActive;
    private volatile bool pPlayerRendererPending;
    private readonly PPlayer pViewerPlayer = new();
    private LMediaInfo? pViewerMediaInfo;
    private Point? pViewerCropPoint;
    private PViewerTool pViewerTool;
    private LNeutralTarget pViewerNeutralTarget;
    private int pViewerNeutralSerial;
    private bool pViewerNeutralPlaying;
    private Size? pViewerCropRatio;
    private readonly Path pViewerCropShade;
    private readonly Rectangle[] pViewerCropHandles = new Rectangle[8];
    private Rect pViewerCropOrigin;
    private Point pViewerCropGrab;
    private bool pViewerCropDrag;
    private int pViewerEdgeX;
    private int pViewerEdgeY;
    private int pViewerCropDrive = -1;
    private int pViewerAnchorX = -1;
    private int pViewerAnchorY = -1;
    private int pViewerLoadSerial;
    private string? pViewerLoadPath;
    private readonly LMediaLoad pViewerMediaProbe = new();
    private double pViewerVolume = LPreference.LPreferenceStateCurrent.LPreferenceVolume;
    private bool pViewerAudioAllowed;
    private bool pViewerCommandActive;
    private bool pViewerResumeInactive;
    private bool pViewerEndReached;
    private bool pViewerUnloaded;
    private bool pViewerDragActive;
    private readonly List<string> pViewerSeekTrace = [];
    private int pViewerTraceCount;
    private TimeSpan pViewerTraceFinal;

    public event Action<LCargo>? PViewerMediaChange;
    public event Action<TimeSpan>? PViewerClockTick;
    public event Action<Rect?>? PCropVideoChange;

    internal bool PViewerSurfaceMatch(nint pViewerHandle)
    {
        if (pViewerHandle == nint.Zero || pViewerFlyleafHost is null)
        {
            return false;
        }

        try
        {
            return pViewerHandle == PViewerWindowHandle(pViewerFlyleafHost.Surface)
                || pViewerHandle == PViewerWindowHandle(pViewerFlyleafHost.Overlay);
        }
        catch
        {
            return false;
        }
    }

    private static nint PViewerWindowHandle(Window? pViewerWindow) =>
        pViewerWindow is null ? nint.Zero : new System.Windows.Interop.WindowInteropHelper(pViewerWindow).Handle;

    public string? PViewerSourcePath { get; private set; }
    public Rect? PCropVideo { get; private set; }
    public double PViewerVolumeCurrent => pViewerVolume;
    public LPreviewEngine PViewerEngineCurrent { get; private set; } = LPreviewEngine.LPreviewEngineFlyleaf;
    public event Action? PViewerEngineChange;
    public event Action<bool>? PViewerPlayingChange;
    public event Action? PViewerPreviewChange;
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
            Visibility = Visibility.Collapsed
        };

        pViewerOverlay = new Canvas
        {
            Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)),
            Focusable = true,
            AllowDrop = true
        };
        pViewerCropShade = new Path
        {
            Fill = new SolidColorBrush(Color.FromArgb(0x66, 0x00, 0x00, 0x00)),
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed
        };

        pViewerOverlay.Children.Add(pViewerCropShade);
        pViewerOverlay.Children.Add(pViewerCropBox);
        PCropHandlesBuild();
        pViewerCropBox.MouseLeftButtonDown += PCropBodyHandle;
        pViewerOverlay.MouseLeftButtonDown += PCropPressHandle;
        pViewerOverlay.MouseMove += PCropMoveHandle;
        pViewerOverlay.MouseLeftButtonUp += PCropReleaseHandle;
        pViewerOverlay.SizeChanged += PCropSizeHandle;
        pViewerOverlay.KeyDown += PViewerKeyHandle;

        pViewerCloseButton = PViewerCloseBuild();
        pViewerPreviewButton = PViewerPreviewBuild();
        pViewerAudioSwitch = PViewerAudioBuild();
        pViewerEngineSurface = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 16, 0, 0)
        };
        pViewerEngineOverlay = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(16, 16, 0, 0)
        };
        PViewerEngineShow();
        Cadroue.Infrastructure.LRenderer.LRendererEngineChange += PViewerEngineShow;

        PDropHandlersAdd();

        pViewerClockTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        pViewerClockTimer.Tick += PViewerClockHandle;
        pViewerMediaProbe.LMediaLoadCompleted += PViewerLoadHandle;
    }

    private Button PViewerCloseBuild()
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
                Source = PIcon.PIconRead("/PAssets/PPanels/PViewerClose.svg", new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
                Stretch = Stretch.Uniform
            },
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 16, 0),
            ToolTip = LLocalization.LLocalizationTextRead("Viewer.Unload.Tooltip"),
            Visibility = Visibility.Collapsed,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PViewerMediaClose();
        return pButton;
    }

    private Button PViewerPreviewBuild()
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 12,
                Height = 12,
                Source = PIcon.PIconRead("/PAssets/PPanels/PViewerPreview.svg", new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
                Stretch = Stretch.Uniform
            },
            Width = 24,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 16, 44, 0),
            ToolTip = LLocalization.LLocalizationTextRead("Viewer.Preview.Tooltip"),
            Visibility = Visibility.Collapsed,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PLoupeShow();
        return pButton;
    }

    private void PLoupeShow()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.Activate();
            return;
        }

        if (Window.GetWindow(this) is not { } pViewerOwner)
        {
            return;
        }

        PSLoupe.PSLoupeShow(pViewerOwner, this);
    }

    internal void PViewerLoupeSync(TimeSpan pViewerPosition, bool pViewerPlaying)
    {
        PViewerPlaybackUpdate(pViewerPlaying, pViewerPosition);
        PViewerClockTick?.Invoke(pViewerPosition);
    }

    public bool PViewerPlayingRead() => LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;

    public TimeSpan PViewerPositionRead() => LPreviewStateCurrent.LPlaybackState.LPlaybackPosition;

    public void PViewerPlay()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupePlay();
            return;
        }

        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            return;
        }

        if (pViewerEndReached)
        {
            pViewerEndReached = false;
            pViewerPlayer.PPlayerSeek(TimeSpan.Zero);
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPlay();
        PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    public void PViewerPause()
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupePause();
            return;
        }

        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPause();
        PViewerPlaybackUpdate(false, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Stop();
    }

    public void PViewerSeek(TimeSpan playbackPosition)
    {
        if (pViewerLoupe is not null)
        {
            pViewerLoupe.PSLoupeSeek(playbackPosition);
            PViewerLoupeSync(playbackPosition, LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying);
            return;
        }

        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerEndReached = false;
        try
        {
            PPlayerAccurateSeek(playbackPosition);
        }
        catch (Exception pViewerSeekException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Preview seek to {playbackPosition:hh\\:mm\\:ss\\.fff} was rejected: {pViewerSeekException.Message}");
            return;
        }

        PViewerPlaybackUpdate(null, playbackPosition);
    }

    public void PViewerDragSet(bool pViewerDragging)
    {
        if (pViewerDragging)
        {
            if (!pViewerDragActive)
            {
                pViewerSeekTrace.Clear();
                pViewerTraceCount = 0;
            }

            pViewerDragActive = true;
            return;
        }

        pViewerDragActive = false;
        if (pViewerTraceCount == 0)
        {
            return;
        }

        string pViewerSummary = pViewerTraceCount == 1
            ? $"Seek accurate to {pViewerTraceFinal:hh\\:mm\\:ss\\.fff}"
            : $"Seek accurate while dragging to {pViewerTraceFinal:hh\\:mm\\:ss\\.fff} ({pViewerTraceCount} requests)";
        LTrace.LTraceRecord(
            LTraceKind.LTraceUi,
            pViewerSummary,
            string.Join(Environment.NewLine, pViewerSeekTrace));
        pViewerSeekTrace.Clear();
        pViewerTraceCount = 0;
    }

    private void PViewerSeekRecord(TimeSpan pViewerPosition, string pViewerDetail)
    {
        string pViewerSummary = $"Seek accurate to {pViewerPosition:hh\\:mm\\:ss\\.fff}";
        if (!pViewerDragActive)
        {
            LTrace.LTraceRecord(LTraceKind.LTraceUi, pViewerSummary, pViewerDetail);
            return;
        }

        if (!LTrace.LTraceCheck(LTraceKind.LTraceUi))
        {
            return;
        }

        string pViewerTime = DateTimeOffset.Now.ToString(
            "HH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture);
        pViewerSeekTrace.Add($"{pViewerTime}  {pViewerSummary}");
        pViewerSeekTrace.Add($"{new string(' ', 14)}{pViewerDetail}");
        pViewerTraceCount++;
        pViewerTraceFinal = pViewerPosition;
    }

    public void PViewerVolumeSet(double volume)
    {
        if (pViewerLoupe is not null)
        {
            pViewerVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
            if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
                LPreference.LPreferenceVolumeSet(pViewerVolume);
            pViewerLoupe.PSLoupeVolumeSet(pViewerVolume);
            return;
        }

        if (!pViewerCommandActive) return;
        pViewerVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
        if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
            LPreference.LPreferenceVolumeSet(pViewerVolume);
        if (!pViewerPlayer.PPlayerReady)
        {
            return;
        }

        pViewerPlayer.PPlayerVolumeSet(pViewerVolume);
    }

    public void PViewerAudioSet(bool pAudioOnlyAllowed)
    {
        pViewerAudioAllowed = pAudioOnlyAllowed;
    }

    public void PViewerCommandSet(bool pCommandActive)
    {
        if (pViewerUnloaded || pViewerCommandActive == pCommandActive)
        {
            return;
        }

        PViewerHostRecord($"command set {(pCommandActive ? "on" : "off")}");
        if (!pCommandActive)
        {
            PPlayerSuspend();
            pViewerCommandActive = false;
            pViewerLoadSerial++;
            pViewerClockTimer.Stop();
            return;
        }

        PViewerHostBuild();
        pViewerCommandActive = true;
        string? pViewerSourcePath = PViewerSourcePath;
        if (PViewerEngineSelect() && pViewerSourcePath is not null)
        {
            PPlayerVideoLoad(pViewerSourcePath);
            return;
        }

        PPlayerResume();
    }

    private void PViewerPreviewApply()
    {
        if (pViewerMpvActive)
        {
            PViewerMpvUpdate();
            PViewerPreviewChange?.Invoke();
            return;
        }

        LPreview.LPreviewApply(pViewerPlayer.PPlayerFlyleafPlayer, PViewerRenderRead());
        PPlayerColorRecord(pViewerPlayer.PPlayerFlyleafPlayer);
        PViewerPreviewChange?.Invoke();
    }

    public LPreviewState PViewerRenderRead() =>
        PCropActive
            ? LPreviewStateCurrent
            : LPreviewStateCurrent.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate());

    public string PViewerAudioRead() => PViewerAudioResolve();

    private void PViewerPreviewRestore()
    {
        LRotateFlip pViewerRotate = LPreviewStateCurrent.LRotateFlip;
        LTraceLog.LTraceInfoRecord(
            $"Viewer preview restored: rotate {pViewerRotate.LRotateKind}, "
            + $"H {pViewerRotate.LRotateFlipHorizontal}, V {pViewerRotate.LRotateFlipVertical}");
        LPreview.LPreviewRestore(pViewerPlayer.PPlayerFlyleafPlayer, LPreviewStateCurrent);
    }

    public TimeSpan PViewerDurationRead() => pViewerMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero;

    public void PViewerRotateSet(LRotateFlip pRotateFlip)
    {
        LPreviewStateCurrent = LPreviewStateCurrent.LRotateFlipChange(pRotateFlip);
        LTraceLog.LTraceInfoRecord(
            $"Viewer rotate/flip set: rotate {pRotateFlip.LRotateKind}, "
            + $"H {pRotateFlip.LRotateFlipHorizontal}, V {pRotateFlip.LRotateFlipVertical}, "
            + $"player {(pViewerPlayer.PPlayerReady ? "ready" : "none")}, overlay remapped");
        PViewerPreviewApply();
        PCropOverlayUpdate();
    }

    public void PViewerColorSet(LColor pColor)
    {
        LPreviewStateCurrent = LPreviewStateCurrent.LColorChange(pColor);
        PViewerPreviewApply();
    }
}
