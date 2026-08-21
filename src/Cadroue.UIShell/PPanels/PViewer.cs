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
    private bool pViewerUnloaded;

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

    public bool PViewerPlayingRead() => LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying;

    public void PViewerPlay()
    {
        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            return;
        }

        pViewerResumeInactive = false;
        pViewerPlayer.PPlayerPlay();
        PViewerPlaybackUpdate(true, pViewerPlayer.PPlayerTimeRead());
        pViewerClockTimer.Start();
    }

    public void PViewerPause()
    {
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
        if (!pViewerCommandActive || !pViewerPlayer.PPlayerReady)
        {
            return;
        }

        PPlayerAccurateSeek(playbackPosition);
        PViewerPlaybackUpdate(null, playbackPosition);
    }

    public void PViewerVolumeSet(double volume)
    {
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
        if (pViewerCommandActive == pCommandActive)
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

    public void PViewerSourceOpen(string sourcePath)
    {
        LTraceLog.LTraceInfoRecord(
            $"Viewer source open requested '{System.IO.Path.GetFileName(sourcePath)}'",
            $"command={(pViewerCommandActive ? "active" : "INACTIVE")}, unloaded={pViewerUnloaded}, "
            + $"engine={PViewerEngineCurrent}, path={sourcePath}");

        if (!pViewerCommandActive || string.IsNullOrWhiteSpace(sourcePath))
        {
            LTraceLog.LTraceWarningRecord(
                $"Viewer source open refused: {(pViewerCommandActive ? "empty path" : "viewer command inactive (tab not the front workspace)")}");
            return;
        }

        if (LLibrarian.LLibrarianFileCheck(sourcePath))
        {
            if (PViewerSidecarResolve(sourcePath) is not { } pResolvedPath)
            {
                LTraceLog.LTraceWarningRecord("Viewer source open refused: sidecar (.cad) source could not be resolved");
                return;
            }

            sourcePath = pResolvedPath;
        }

        LPreference.LPreferenceMediaSet(sourcePath);
        if (!PCropPersistent)
        {
            LPreviewStateCurrent = LPreviewStateCurrent.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate());
        }

        PPlayerVideoLoad(sourcePath);
    }

    private string? PViewerSidecarResolve(string pSidecarPath)
    {
        LSidecarSourceResult? pResult = LLibrarian.LLibrarianSourceResolve(pSidecarPath);
        if (pResult is null)
        {
            MessageBox.Show(
                LLocalization.LLocalizationTextRead("Viewer.Sidecar.ReadError"),
                LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return null;
        }

        if (pResult.LSidecarResultVerified)
        {
            return pResult.LSidecarResultPath;
        }

        if (pResult.LSidecarResultKind != LSidecarSourceKind.LSidecarSourceMissing
            && MessageBox.Show(
                LLocalization.LLocalizationFormat("Viewer.Sidecar.MismatchFound", pResult.LSidecarResultPath),
                LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            return pResult.LSidecarResultPath;
        }

        return PViewerSidecarFind(pSidecarPath, pResult.LSidecarResultName);
    }

    private string? PViewerSidecarFind(string pSidecarPath, string pSidecarFileName)
    {
        var pDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = LLocalization.LLocalizationFormat("Viewer.Locate.Title", pSidecarFileName),
            FileName = pSidecarFileName,
            Filter = LLocalization.LLocalizationTextRead("Viewer.Dialog.MediaFilter")
        };

        if (pDialog.ShowDialog() != true)
        {
            return null;
        }

        if (LLibrarian.LLibrarianSourceMatch(pDialog.FileName, pSidecarPath))
        {
            return pDialog.FileName;
        }

        return MessageBox.Show(
            LLocalization.LLocalizationTextRead("Viewer.Sidecar.MismatchSelected"),
            LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question) == MessageBoxResult.Yes
            ? pDialog.FileName
            : null;
    }

    private void PViewerPreviewApply()
    {
        if (pViewerMpvActive)
        {
            PViewerMpvUpdate();
            return;
        }

        LPreview.LPreviewApply(pViewerPlayer.PPlayerFlyleafPlayer, PViewerRenderRead());
        PPlayerColorRecord(pViewerPlayer.PPlayerFlyleafPlayer);
    }

    private LPreviewState PViewerRenderRead() =>
        PCropActive
            ? LPreviewStateCurrent
            : LPreviewStateCurrent.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate());

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
