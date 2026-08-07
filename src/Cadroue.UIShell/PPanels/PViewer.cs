using System.Windows;
using System.Windows.Controls;
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

using Cadroue.Infrastructure;

using Cadroue.MigrationInterface;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PViewer : PPanel
{
    private readonly Border pViewerSurface;
    private readonly Button pViewerCloseButton;
    private readonly FlyleafHost pViewerFlyleafHost;
    private readonly Canvas pViewerOverlay;
    private readonly Rectangle pViewerCropBox;
    private readonly DispatcherTimer pViewerClockTimer;
    private volatile bool pPlayerAccurateActive;
    private volatile bool pPlayerRendererPending;
    private Player? pViewerPlayer;
    private LMediaInfo? pViewerMediaInfo;
    private Point? pViewerCropPoint;
    private bool pViewerCropArmed;
    private Size? pViewerCropRatio;
    private readonly Path pViewerCropShade;
    private readonly Rectangle[] pViewerCropHandles = new Rectangle[8];
    private Rect pViewerCropOrigin;
    private Point pViewerCropGrab;
    private bool pViewerCropDrag;
    private int pViewerEdgeX;
    private int pViewerEdgeY;
    private int pViewerLoadSerial;
    private double pViewerVolume = LPreference.LPreferenceStateCurrent.LPreferenceVolume;
    private bool pViewerAudioAllowed;
    private bool pViewerCommandActive;
    private bool pViewerResumeInactive;
    private bool pViewerUnloaded;

    public event Action<LCargo>? PViewerMediaChange;
    public event Action<TimeSpan>? PViewerClockTick;
    public event Action<Rect?>? PCropVideoChange;

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

        pViewerCloseButton = PViewerCloseBuild();

        var pViewerOverlayHost = new Grid();
        pViewerOverlayHost.Children.Add(pViewerOverlay);
        pViewerOverlayHost.Children.Add(pViewerCloseButton);

        pViewerFlyleafHost = new FlyleafHost
        {
            Content = pViewerOverlayHost,
            VideoBackground = Brushes.White,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None,
            Visibility = Visibility.Collapsed
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
        PViewerHostAttach();
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
        if (!pViewerCommandActive || pViewerPlayer is null)
        {
            return;
        }

        if (LPreviewStateCurrent.LPlaybackState.LPlaybackStatePlaying)
        {
            return;
        }

        pViewerResumeInactive = false;
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

        pViewerResumeInactive = false;
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

        PPlayerAccurateSeek(pViewerPlayer, playbackPosition);
        PViewerPlaybackUpdate(null, playbackPosition);
    }

    public void PViewerVolumeSet(double volume)
    {
        if (!pViewerCommandActive) return;
        pViewerVolume = LPreferenceState.LPreferenceVolumeClamp(volume);
        if (LPreference.LPreferenceStateCurrent.LPreferenceVolumeUnified)
            LPreference.LPreferenceVolumeSet(pViewerVolume);
        if (pViewerPlayer is null)
        {
            return;
        }

        pViewerPlayer.Audio.Volume = (int)Math.Round(pViewerVolume);
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

        LPreference.LPreferenceMediaSet(sourcePath);
        LPreviewStateCurrent = LPreviewStateCurrent.LRotateFlipChange(LRotateFlip.LRotateDefaultCreate());
        _ = PPlayerVideoLoad(sourcePath);
    }

    private string? PViewerSidecarResolve(string pSidecarPath)
    {
        LSidecar? pSidecar = LSidecarStore.LSidecarRead(pSidecarPath);
        if (pSidecar is null)
        {
            MessageBox.Show(
                LLocalization.LLocalizationTextRead("Viewer.Sidecar.ReadError"),
                LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
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
                LLocalization.LLocalizationFormat("Viewer.Sidecar.MismatchFound", pResult.LSidecarResultPath),
                LLocalization.LLocalizationTextRead("Viewer.Dialog.OpenTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            return pResult.LSidecarResultPath;
        }

        return PViewerSidecarFind(pSidecar);
    }

    private string? PViewerSidecarFind(LSidecar pSidecar)
    {
        var pDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = LLocalization.LLocalizationFormat("Viewer.Locate.Title", pSidecar.LSidecarSource.LSidecarFileName),
            FileName = pSidecar.LSidecarSource.LSidecarFileName,
            Filter = LLocalization.LLocalizationTextRead("Viewer.Dialog.MediaFilter")
        };

        if (pDialog.ShowDialog() != true)
        {
            return null;
        }

        if (LSidecarSource.LSidecarVerifyCheck(pDialog.FileName, pSidecar.LSidecarSource))
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
        LPreview.LPreviewApply(pViewerPlayer, LPreviewStateCurrent);
        PPlayerColorRecord(pViewerPlayer);
    }

    private void PViewerPreviewRestore()
    {
        LRotateFlip pViewerRotate = LPreviewStateCurrent.LRotateFlip;
        LTraceLog.LTraceInfoRecord(
            $"Viewer preview restored: rotate {pViewerRotate.LRotateKind}, "
            + $"H {pViewerRotate.LRotateFlipHorizontal}, V {pViewerRotate.LRotateFlipVertical}");
        LPreview.LPreviewRestore(pViewerPlayer, LPreviewStateCurrent);
    }

    public TimeSpan PViewerDurationRead() => pViewerMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero;

    public void PViewerRotateSet(LRotateFlip pRotateFlip)
    {
        bool pRotateChanged = LPreviewStateCurrent.LRotateFlip.LRotateKind != pRotateFlip.LRotateKind;
        LPreviewStateCurrent = LPreviewStateCurrent.LRotateFlipChange(pRotateFlip);
        LTraceLog.LTraceInfoRecord(
            $"Viewer rotate/flip set: rotate {pRotateFlip.LRotateKind}, "
            + $"H {pRotateFlip.LRotateFlipHorizontal}, V {pRotateFlip.LRotateFlipVertical}, "
            + $"player {(pViewerPlayer is null ? "none" : "ready")}, "
            + $"{(pRotateChanged ? "rotation changed: crop hidden" : "rotation same: overlay kept")}");
        PViewerPreviewApply();

        if (pRotateChanged)
        {
            PCropHide();
            return;
        }

        PCropOverlayUpdate();
    }

    public void PViewerColorSet(LColor pColor)
    {
        LPreviewStateCurrent = LPreviewStateCurrent.LColorChange(pColor);
        PViewerPreviewApply();
    }
}
