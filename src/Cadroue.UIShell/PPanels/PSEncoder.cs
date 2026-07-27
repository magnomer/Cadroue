using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder : Window
{
    private const int PSGrabberBorderPixels = 8;
    private const int PSGrabberLeft = 1;
    private const int PSGrabberRight = 2;
    private const int PSGrabberTop = 4;
    private const int PSGrabberBottom = 8;
    private const int PSEncoderDwmPreference = 33;
    private const int PSEncoderDwmRound = 2;
    private const int PSEncoderDwmCaption = 35;
    private const int PSEncoderColor = 0x00F7E8DC;

    internal const double PSEncoderWidthDefault = 820;
    internal const double PSEncoderHeightDefault = 700;
    internal const double PSEncoderWidthMinimum = 680;
    internal const double PSEncoderHeightMinimum = 520;

    private readonly LExportSpecificState lsExportSpecificSource;
    private readonly LExportSpecificState lsExportSpecificEdit;
    private readonly System.Action pSummaryRefresh;
    private readonly PToken psNameBox;
    private readonly ComboBox psContainerCombo;
    private readonly ComboBox psModeCombo;
    private readonly ComboBox psVideoStreamCombo;
    private readonly ComboBox psAudioStreamCombo;
    private readonly ComboBox psVideoModeCombo;
    private readonly ComboBox psAudioModeCombo;
    private readonly ComboBox psVideoEncoderCombo;
    private readonly ComboBox psVideoRateCombo;
    private readonly ComboBox psLocationCombo;
    private readonly ComboBox psVideoSizeCombo;
    private readonly ComboBox psVideoFpsCombo;
    private readonly ComboBox psPixelCombo;
    private readonly ComboBox psAudioEncoderCombo;
    private readonly ComboBox psAudioBitrateCombo;
    private readonly ComboBox psAudioSampleCombo;
    private readonly ComboBox psAudioChannelCombo;
    private readonly StackPanel psVideoRowsPanel;
    private readonly StackPanel psVideoEncodePanel;
    private readonly StackPanel psAudioEncodePanel;
    private readonly TextBlock psVideoNotice;
    private readonly TextBlock psAudioNotice;
    private readonly Dictionary<string, ComboBox> psVideoExtraCombos;
    private TextBox? psVideoQualityBox;
    private ComboBox? psVideoSpeedCombo;
    private bool psVideoRowsBusy;
    private string psCodecLog = "Verification has not run yet.";
    private string? psEncoderFolderPath;
    private bool psGrabberActive;
    private int psGrabberDirection;
    private Point psGrabberStartPointer;
    private Rect psGrabberStartBounds;

    private static readonly Brush PLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush PTextBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush PMutedBrush = new SolidColorBrush(Color.FromRgb(0x62, 0x6F, 0x83));

    public PSEncoder(LExportSpecificState lsExportSpecificState, System.Action pRefresh)
    {
        lsExportSpecificSource = lsExportSpecificState;
        lsExportSpecificEdit = lsExportSpecificState.LPresetClone();
        pSummaryRefresh = pRefresh;
        psNameBox = new PToken { Text = lsExportSpecificEdit.Name, MinWidth = 320 };
        psContainerCombo = PSComboBuild(lsExportSpecificEdit.Container, "MP4", "Matroska", "MOV", "WebM", "M4A", "MP3", "WAV", "FLAC", "OGG", "All FFmpeg formats...");
        psModeCombo = PSComboBuild(lsExportSpecificEdit.ExportMode, "Smart export", "Remux only", "Re-encode");
        psVideoStreamCombo = PSComboBuild(lsExportSpecificEdit.VideoStream, "Include", "Exclude");
        psAudioStreamCombo = PSComboBuild(lsExportSpecificEdit.AudioStream, "Include first audio track", "Include all audio tracks", "Exclude");
        psVideoModeCombo = PSComboBuild(lsExportSpecificEdit.VideoMode, "Auto", "Copy", "Encode", "Exclude");
        psAudioModeCombo = PSComboBuild(lsExportSpecificEdit.AudioMode, "Auto", "Copy", "Encode", "Exclude");

        psVideoEncoderCombo = PSComboBuild(lsExportSpecificEdit.VideoEncoder, PSCodecItemsRead());
        LCapabilityCodec pVideoCodec = LCapability.LCapabilityRead(PSCodecValueRead(PSComboTextRead(psVideoEncoderCombo)));
        psVideoRateCombo = PSComboBuild(lsExportSpecificEdit.VideoRateControl, pVideoCodec.CapabilityModeLabels);
        psVideoRowsPanel = new StackPanel();
        psVideoEncodePanel = new StackPanel();
        psAudioEncodePanel = new StackPanel();
        psVideoNotice = PSScopeNoticeBuild();
        psAudioNotice = PSScopeNoticeBuild();
        psVideoExtraCombos = new Dictionary<string, ComboBox>(StringComparer.Ordinal);

        psLocationCombo = PSComboBuild(lsExportSpecificEdit.Location, "Same as source", "Custom folder");
        psEncoderFolderPath = string.IsNullOrWhiteSpace(lsExportSpecificEdit.LocationFolder)
            ? null
            : lsExportSpecificEdit.LocationFolder;
        psVideoSizeCombo = PSComboBuild(lsExportSpecificEdit.VideoSize, "Same as source", "3840 × 2160", "2560 × 1440", "1920 × 1080", "1280 × 720", "854 × 480", "Custom");
        psVideoFpsCombo = PSComboBuild(lsExportSpecificEdit.VideoFps, "Same as source", "60", "50", "30", "25", "24", "Custom");
        psPixelCombo = PSComboBuild(lsExportSpecificEdit.PixelFormat, "Auto", "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le", "yuv444p10le");
        psAudioEncoderCombo = PSComboBuild(lsExportSpecificEdit.AudioEncoder, "AAC", "MP3 / libmp3lame", "Opus / libopus", "FLAC", "PCM 16-bit / pcm_s16le", "PCM 24-bit / pcm_s24le");
        psAudioBitrateCombo = PSComboBuild(lsExportSpecificEdit.AudioBitrate, "96k", "128k", "160k", "192k", "256k", "320k", "Custom");
        psAudioSampleCombo = PSComboBuild(lsExportSpecificEdit.AudioSampleRate, "Same as source", "44100", "48000", "88200", "96000", "Custom");
        psAudioChannelCombo = PSComboBuild(lsExportSpecificEdit.AudioChannels, "Same as source", "Mono", "Stereo", "5.1", "Custom");

        Title = "Specific Export Settings";
        Width = App.LPreferenceStateCurrent.LPreferenceEncoderWidth;
        Height = App.LPreferenceStateCurrent.LPreferenceEncoderHeight;
        MinWidth = PSEncoderWidthMinimum;
        MinHeight = PSEncoderHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSSheetBodyFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        Content = PSEncoderBuild();
        PSEncoderPositionRestore(App.LPreferenceStateCurrent);
        PSGrabberHandlersAdd();
    }

    private UIElement PSEncoderBuild()
    {
        var pRoot = new Grid { Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)) };
        pRoot.Children.Add(PSSheetControlBuild());
        pRoot.Children.Add(PSCasementOverlayBuild());
        return pRoot;
    }

    private void PSEncoderApply()
    {
        lsExportSpecificEdit.Name = string.IsNullOrWhiteSpace(psNameBox.Text) ? "{OriginalName}_export" : psNameBox.Text.Trim();
        lsExportSpecificEdit.Container = PSComboTextRead(psContainerCombo);
        lsExportSpecificEdit.ExportMode = PSComboTextRead(psModeCombo);
        lsExportSpecificEdit.VideoStream = PSComboTextRead(psVideoStreamCombo);
        lsExportSpecificEdit.AudioStream = PSComboTextRead(psAudioStreamCombo);
        lsExportSpecificEdit.VideoMode = PSComboTextRead(psVideoModeCombo);
        lsExportSpecificEdit.AudioMode = PSComboTextRead(psAudioModeCombo);
        lsExportSpecificEdit.VideoEncoder = PSComboTextRead(psVideoEncoderCombo);
        lsExportSpecificEdit.VideoRateControl = PSComboTextRead(psVideoRateCombo);
        lsExportSpecificEdit.VideoQuality = psVideoQualityBox?.Text.Trim() ?? string.Empty;
        lsExportSpecificEdit.VideoSpeedPreset = psVideoSpeedCombo is null ? string.Empty : PSComboTextRead(psVideoSpeedCombo);
        lsExportSpecificEdit.Location = PSComboTextRead(psLocationCombo);
        lsExportSpecificEdit.LocationFolder = psEncoderFolderPath ?? string.Empty;
        lsExportSpecificEdit.VideoSize = PSComboTextRead(psVideoSizeCombo);
        lsExportSpecificEdit.VideoFps = PSComboTextRead(psVideoFpsCombo);
        lsExportSpecificEdit.PixelFormat = PSComboTextRead(psPixelCombo);
        lsExportSpecificEdit.VideoExtras = psVideoExtraCombos.ToDictionary(
            pExtra => pExtra.Key, pExtra => PSComboTextRead(pExtra.Value), StringComparer.Ordinal);
        lsExportSpecificEdit.AudioEncoder = PSComboTextRead(psAudioEncoderCombo);
        lsExportSpecificEdit.AudioBitrate = PSComboTextRead(psAudioBitrateCombo);
        lsExportSpecificEdit.AudioSampleRate = PSComboTextRead(psAudioSampleCombo);
        lsExportSpecificEdit.AudioChannels = PSComboTextRead(psAudioChannelCombo);
        lsExportSpecificSource.LPresetCopy(lsExportSpecificEdit);
        pSummaryRefresh();
    }

    private void PSGrabberHandlersAdd()
    {
        PreviewMouseMove += PSGrabberMoveHandle;
        PreviewMouseLeftButtonDown += PSGrabberPressHandle;
        PreviewMouseLeftButtonUp += PSGrabberReleaseHandle;
        LostMouseCapture += PSGrabberCaptureHandle;
        Closed += PSEncoderCloseHandle;
    }

    private void PSGrabberHandlersRemove()
    {
        PreviewMouseMove -= PSGrabberMoveHandle;
        PreviewMouseLeftButtonDown -= PSGrabberPressHandle;
        PreviewMouseLeftButtonUp -= PSGrabberReleaseHandle;
        LostMouseCapture -= PSGrabberCaptureHandle;
        Closed -= PSEncoderCloseHandle;
    }

    private void PSGrabberPressHandle(object sender, MouseButtonEventArgs e)
    {
        int pDirection = PSGrabberDirectionRead(e.GetPosition(this));
        if (WindowState != WindowState.Normal || pDirection == 0)
        {
            return;
        }

        psGrabberActive = true;
        psGrabberDirection = pDirection;
        psGrabberStartPointer = PSGrabberPointerRead(e);
        psGrabberStartBounds = new Rect(Left, Top, ActualWidth, ActualHeight);
        Mouse.Capture(this);
        e.Handled = true;
    }

    private void PSGrabberMoveHandle(object sender, MouseEventArgs e)
    {
        if (psGrabberActive)
        {
            PSGrabberApply(PSGrabberPointerRead(e));
            e.Handled = true;
            return;
        }

        int pDirection = WindowState == WindowState.Normal ? PSGrabberDirectionRead(e.GetPosition(this)) : 0;
        Cursor = pDirection == 0 ? null : PSGrabberCursorRead(pDirection);
    }

    private void PSGrabberReleaseHandle(object sender, MouseButtonEventArgs e)
    {
        if (!psGrabberActive)
        {
            return;
        }

        psGrabberActive = false;
        Mouse.Capture(null);
        e.Handled = true;
    }

    private void PSGrabberCaptureHandle(object sender, MouseEventArgs e)
    {
        psGrabberActive = false;
    }

    private void PSEncoderPositionRestore(LPreferenceState lPreferenceState)
    {
        if (lPreferenceState.LPreferenceEncoderLeft is not double psLeft
            || lPreferenceState.LPreferenceEncoderTop is not double psTop)
        {
            return;
        }

        double psScreenRight = SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth;
        double psScreenBottom = SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
        if (psLeft >= SystemParameters.VirtualScreenLeft && psTop >= SystemParameters.VirtualScreenTop
            && psLeft + 100 <= psScreenRight && psTop + 40 <= psScreenBottom)
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = psLeft;
            Top = psTop;
        }
    }

    private void PSEncoderCloseHandle(object? sender, System.EventArgs e)
    {
        PSEncoderPositionSave();
        PSGrabberHandlersRemove();
    }

    private void PSEncoderPositionSave()
    {
        Rect psBounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;
        if (double.IsNaN(psBounds.Left) || double.IsNaN(psBounds.Top) || psBounds.Width <= 0 || psBounds.Height <= 0)
        {
            return;
        }

        LPreferenceState lPreferenceState = App.LPreferenceStateCurrent.LPreferenceClone();
        lPreferenceState.LPreferenceEncoderLeft = psBounds.Left;
        lPreferenceState.LPreferenceEncoderTop = psBounds.Top;
        lPreferenceState.LPreferenceEncoderWidth = psBounds.Width;
        lPreferenceState.LPreferenceEncoderHeight = psBounds.Height;
        App.LPreferenceStateSet(lPreferenceState);
    }

    private int PSGrabberDirectionRead(Point pPoint)
    {
        bool pLeft = pPoint.X >= 0 && pPoint.X < PSGrabberBorderPixels;
        bool pRight = pPoint.X <= ActualWidth && pPoint.X > ActualWidth - PSGrabberBorderPixels;
        bool pTop = pPoint.Y >= 0 && pPoint.Y < PSGrabberBorderPixels;
        bool pBottom = pPoint.Y <= ActualHeight && pPoint.Y > ActualHeight - PSGrabberBorderPixels;
        int pDirection = 0;
        if (pLeft) pDirection |= PSGrabberLeft;
        if (pRight) pDirection |= PSGrabberRight;
        if (pTop) pDirection |= PSGrabberTop;
        if (pBottom) pDirection |= PSGrabberBottom;
        return pDirection;
    }

    private static Cursor PSGrabberCursorRead(int pDirection)
    {
        bool pHorizontal = (pDirection & (PSGrabberLeft | PSGrabberRight)) != 0;
        bool pVertical = (pDirection & (PSGrabberTop | PSGrabberBottom)) != 0;
        if (!pHorizontal || !pVertical)
        {
            return pHorizontal ? Cursors.SizeWE : Cursors.SizeNS;
        }

        bool pLeft = (pDirection & PSGrabberLeft) != 0;
        bool pTop = (pDirection & PSGrabberTop) != 0;
        return pLeft == pTop ? Cursors.SizeNWSE : Cursors.SizeNESW;
    }

    private Point PSGrabberPointerRead(MouseEventArgs e)
    {
        Point pScreenPoint = PointToScreen(e.GetPosition(this));
        return PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice.Transform(pScreenPoint) ?? pScreenPoint;
    }

    private void PSGrabberApply(Point pPointer)
    {
        double pDx = pPointer.X - psGrabberStartPointer.X;
        double pDy = pPointer.Y - psGrabberStartPointer.Y;
        double pLeft = psGrabberStartBounds.Left;
        double pTop = psGrabberStartBounds.Top;
        double pWidth = psGrabberStartBounds.Width;
        double pHeight = psGrabberStartBounds.Height;

        if ((psGrabberDirection & PSGrabberLeft) != 0)
        {
            pWidth = Math.Max(MinWidth, psGrabberStartBounds.Width - pDx);
            pLeft = psGrabberStartBounds.Right - pWidth;
        }

        if ((psGrabberDirection & PSGrabberRight) != 0)
        {
            pWidth = Math.Max(MinWidth, psGrabberStartBounds.Width + pDx);
        }

        if ((psGrabberDirection & PSGrabberTop) != 0)
        {
            pHeight = Math.Max(MinHeight, psGrabberStartBounds.Height - pDy);
            pTop = psGrabberStartBounds.Bottom - pHeight;
        }

        if ((psGrabberDirection & PSGrabberBottom) != 0)
        {
            pHeight = Math.Max(MinHeight, psGrabberStartBounds.Height + pDy);
        }

        Left = pLeft;
        Top = pTop;
        Width = pWidth;
        Height = pHeight;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PSEncoderDwmApply();
    }

    private void PSEncoderDwmApply()
    {
        IntPtr psEncoderHandle = new WindowInteropHelper(this).Handle;
        if (psEncoderHandle == IntPtr.Zero)
        {
            return;
        }

        int psEncoderCornerPreference = PSEncoderDwmRound;
        _ = DwmSetWindowAttribute(
            psEncoderHandle,
            PSEncoderDwmPreference,
            ref psEncoderCornerPreference,
            Marshal.SizeOf<int>());

        int psEncoderCaptionColor = PSEncoderColor;
        _ = DwmSetWindowAttribute(
            psEncoderHandle,
            PSEncoderDwmCaption,
            ref psEncoderCaptionColor,
            Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int windowAttribute,
        ref int attributeValue,
        int attributeByteSize);

}
