using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell.PPanels;

internal sealed partial class PSEncoder : Window
{
    private const int PSEncoderDwmPreference = 33;
    private const int PSEncoderDwmRound = 2;
    private const int PSEncoderDwmCaption = 35;
    private const int PSEncoderColor = 0x00F7E8DC;

    internal const string PSEncoderPlacementKey = "Encoder";

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
    private readonly TextBox psLocationFolderBox;
    private UIElement? psLocationFolderRow;
    private readonly ComboBox psVideoSizeCombo;
    private readonly CheckBox psVideoReactiveBox;
    private readonly TextBox psVideoCustomWidth;
    private readonly TextBox psVideoCustomHeight;
    private UIElement? psVideoCustomRow;
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
    private readonly PSGrabber psEncoderGrabber;

    private static readonly Brush PLineBrush = PSField.PSFieldLine;
    private static readonly Brush PTextBrush = PSField.PSFieldText;
    private static readonly Brush PMutedBrush = PSField.PSFieldMuted;

    public PSEncoder(LExportSpecificState lsExportSpecificState, System.Action pRefresh)
    {
        lsExportSpecificSource = lsExportSpecificState;
        lsExportSpecificEdit = lsExportSpecificState.LPresetClone();
        pSummaryRefresh = pRefresh;
        psNameBox = new PToken { Text = lsExportSpecificEdit.Name, MinWidth = 320 };
        psContainerCombo = PSComboBuild(lsExportSpecificEdit.Container, "Same as source", "MP4", "Matroska", "MOV", "WebM", "M4A", "MP3", "WAV", "FLAC", "OGG", "All FFmpeg formats...");
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

        psLocationCombo = PSComboBuild(lsExportSpecificEdit.Location, "Same as source", "Custom location", "Subfolder");
        bool psLocationSubfolder = string.Equals(lsExportSpecificEdit.Location, "Subfolder", StringComparison.Ordinal);
        psEncoderFolderPath = psLocationSubfolder || string.IsNullOrWhiteSpace(lsExportSpecificEdit.LocationFolder)
            ? null
            : lsExportSpecificEdit.LocationFolder;
        psLocationFolderBox = PSEntryBuild(psLocationSubfolder ? lsExportSpecificEdit.LocationFolder : string.Empty, 220);
        psVideoSizeCombo = PSComboBuild(
            PSVideoLabelRead(lsExportSpecificEdit.VideoSize, lsExportSpecificEdit.VideoSizeReactive),
            lsExportSpecificEdit.VideoSizeReactive ? psVideoReactiveItems : psVideoSizeItems);
        psVideoReactiveBox = PSVideoReactiveBuild(lsExportSpecificEdit.VideoSizeReactive);
        string[] psCustomParts = lsExportSpecificEdit.VideoSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        psVideoCustomWidth = PSEntryBuild(psCustomParts.Length == 2 ? psCustomParts[0] : string.Empty, 110);
        psVideoCustomHeight = PSEntryBuild(psCustomParts.Length == 2 ? psCustomParts[1] : string.Empty, 110);
        psVideoFpsCombo = PSComboBuild(lsExportSpecificEdit.VideoFps, "Same as source", "60", "50", "30", "25", "24", "Custom");
        psPixelCombo = PSComboBuild(lsExportSpecificEdit.PixelFormat, "Auto", "yuv420p", "yuv422p", "yuv444p", "yuv420p10le", "yuv422p10le", "yuv444p10le");
        psAudioEncoderCombo = PSComboBuild(lsExportSpecificEdit.AudioEncoder, "AAC", "MP3 / libmp3lame", "Opus / libopus", "FLAC", "PCM 16-bit / pcm_s16le", "PCM 24-bit / pcm_s24le");
        psAudioBitrateCombo = PSComboBuild(lsExportSpecificEdit.AudioBitrate, "96k", "128k", "160k", "192k", "256k", "320k", "Custom");
        psAudioSampleCombo = PSComboBuild(lsExportSpecificEdit.AudioSampleRate, "Same as source", "44100", "48000", "88200", "96000", "Custom");
        psAudioChannelCombo = PSComboBuild(lsExportSpecificEdit.AudioChannels, "Same as source", "Mono", "Stereo", "5.1", "Custom");

        Title = "Specific Export Settings";
        Width = PSEncoderWidthDefault;
        Height = PSEncoderHeightDefault;
        MinWidth = PSEncoderWidthMinimum;
        MinHeight = PSEncoderHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldBodyFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);
        Content = PSEncoderBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSEncoderPlacementKey);
        psEncoderGrabber = new PSGrabber(this);
        psEncoderGrabber.PSGrabberAttach();
        Closed += PSEncoderCloseHandle;
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
        lsExportSpecificEdit.LocationFolder = string.Equals(lsExportSpecificEdit.Location, "Subfolder", StringComparison.Ordinal)
            ? psLocationFolderBox.Text.Trim()
            : psEncoderFolderPath ?? string.Empty;
        lsExportSpecificEdit.VideoSize = PSVideoSizeRead(PSComboTextRead(psVideoSizeCombo));
        lsExportSpecificEdit.VideoSizeReactive = psVideoReactiveBox.IsChecked == true;
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

    private void PSEncoderCloseHandle(object? sender, System.EventArgs e)
    {
        PSGrabber.PSGrabberPlacementSave(this, PSEncoderPlacementKey);
        psEncoderGrabber.PSGrabberDetach();
        Closed -= PSEncoderCloseHandle;
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
