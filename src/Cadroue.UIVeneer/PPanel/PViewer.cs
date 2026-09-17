using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Cadroue.Media;
using Cadroue.UIVeneer;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;

using Cadroue.Core;
using Cadroue.Application;

using Cadroue.Infrastructure;


namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PViewer : PPanel
{
    private enum PViewerTool
    {
        PViewerToolNone,
        PViewerToolCrop,
        PViewerToolNeutral
    }

    private readonly record struct PViewerIntent(
        string PViewerIntentPath,
        TimeSpan PViewerIntentPosition,
        bool? PViewerIntentPlaying);

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
    private bool pViewerCropLocked;
    private int pViewerEdgeX;
    private int pViewerEdgeY;
    private int pViewerCropDrive = -1;
    private int pViewerAnchorX = -1;
    private int pViewerAnchorY = -1;
    private int pViewerLoadSerial;
    private PViewerIntent? pViewerIntent;
    private readonly LMediaLoad pViewerMediaProbe = new();
    private double pViewerVolume = LPreference.LPreferenceStateCurrent.LPreferenceVolume;
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
    public string? PViewerPendingPath => pViewerIntent?.PViewerIntentPath;
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
                Source = PIcon.PIconRead(
                    "/PAsset/PPanel/PViewerClose.svg",
                    new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
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
                Source = PIcon.PIconRead(
                    "/PAsset/PPanel/PViewerPreview.svg",
                    new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D))),
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
        if (PViewerEngineRestore())
        {
            return;
        }

        PPlayerResume();
    }
}
