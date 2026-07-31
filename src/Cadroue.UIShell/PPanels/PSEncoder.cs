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

    private readonly LPreset lsExportSpecificSource;
    private readonly LPreset lsExportSpecificEdit;
    private readonly System.Action psEncoderSummary;
    private readonly PToken psNameBox;
    private readonly ComboBox psOutputContainerCombo;
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
    private readonly ComboBox psVideoPixelCombo;
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
    private string psCodecLog = LLocalization.LLocalizationTextRead("Encoder.Verification.NotRun");
    private string? psEncoderFolderPath;
    private readonly PSGrabber psEncoderGrabber;

    private static readonly Brush PLineBrush = PSField.PSFieldLine;
    private static readonly Brush PSEncoderTextBrush = PSField.PSFieldText;
    private static readonly Brush PSEncoderMutedBrush = PSField.PSFieldMuted;

    public PSEncoder(LPreset lsExportSpecificState, System.Action pRefresh)
    {
        lsExportSpecificSource = lsExportSpecificState;
        lsExportSpecificEdit = lsExportSpecificState.LPresetClone();
        psEncoderSummary = pRefresh;
        psNameBox = new PToken { PTokenText = lsExportSpecificEdit.LPresetDisplay, MinWidth = 320 };
        psOutputContainerCombo = PSComboBuild(lsExportSpecificEdit.LPresetContainer,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("MP4", "Encoder.Container.MP4"),
            new LLocalizationChoice("Matroska", "Encoder.Container.Matroska"),
            new LLocalizationChoice("MOV", "Encoder.Container.MOV"),
            new LLocalizationChoice("WebM", "Encoder.Container.WebM"),
            new LLocalizationChoice("M4A", "Encoder.Container.M4A"),
            new LLocalizationChoice("MP3", "Encoder.Container.MP3"),
            new LLocalizationChoice("WAV", "Encoder.Container.WAV"),
            new LLocalizationChoice("FLAC", "Encoder.Container.FLAC"),
            new LLocalizationChoice("OGG", "Encoder.Container.OGG"),
            new LLocalizationChoice("All FFmpeg formats...", "Encoder.Format.All"));
        psModeCombo = PSComboBuild(lsExportSpecificEdit.LPresetExportMode,
            new LLocalizationChoice("Smart export", "Encoder.Mode.Smart"),
            new LLocalizationChoice("Remux only", "Encoder.Mode.Remux"),
            new LLocalizationChoice("Re-encode", "Encoder.Mode.Encode"));
        psVideoStreamCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideoStream,
            new LLocalizationChoice("Include", "Encoder.Stream.Include"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psAudioStreamCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudioStream,
            new LLocalizationChoice("Include first audio track", "Encoder.Stream.FirstAudio"),
            new LLocalizationChoice("Include all audio tracks", "Encoder.Stream.AllAudio"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psVideoModeCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideoMode,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psAudioModeCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudioMode,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));

        psVideoEncoderCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideoEncoder, PSCodecItemsRead());
        LCapabilityCodec pVideoCodec = LCapability.LCapabilityRead(PSCodecValueRead(PSComboTextRead(psVideoEncoderCombo)));
        psVideoRateCombo = PSComboBuild(lsExportSpecificEdit.LPresetRateControl, pVideoCodec.LCapabilityModeLabels);
        psVideoRowsPanel = new StackPanel();
        psVideoEncodePanel = new StackPanel();
        psAudioEncodePanel = new StackPanel();
        psVideoNotice = PSAudioNoticeBuild();
        psAudioNotice = PSAudioNoticeBuild();
        psVideoExtraCombos = new Dictionary<string, ComboBox>(StringComparer.Ordinal);

        psLocationCombo = PSComboBuild(lsExportSpecificEdit.LPresetLocation,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("Custom location", "Encoder.Location.Custom"),
            new LLocalizationChoice("Subfolder", "Encoder.Location.Subfolder"),
            new LLocalizationChoice("Sibling", "Encoder.Location.Sibling"));
        bool psLocationNamed = PSLocationNamedCheck(lsExportSpecificEdit.LPresetLocation);
        psEncoderFolderPath = psLocationNamed || string.IsNullOrWhiteSpace(lsExportSpecificEdit.LPresetLocationFolder)
            ? null
            : lsExportSpecificEdit.LPresetLocationFolder;
        psLocationFolderBox = PSEntryBuild(psLocationNamed ? lsExportSpecificEdit.LPresetLocationFolder : string.Empty, 220);
        psVideoSizeCombo = PSComboBuild(
            PSVideoLabelRead(lsExportSpecificEdit.LPresetVideoSize, lsExportSpecificEdit.LPresetSizeReactive),
            PSVideoChoicesRead(lsExportSpecificEdit.LPresetSizeReactive));
        psVideoReactiveBox = PSVideoReactiveBuild(lsExportSpecificEdit.LPresetSizeReactive);
        string[] psCustomParts = lsExportSpecificEdit.LPresetVideoSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        psVideoCustomWidth = PSEntryBuild(psCustomParts.Length == 2 ? psCustomParts[0] : string.Empty, 110);
        psVideoCustomHeight = PSEntryBuild(psCustomParts.Length == 2 ? psCustomParts[1] : string.Empty, 110);
        psVideoFpsCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideoFps,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("60", "Encoder.FPS.Frames60"),
            new LLocalizationChoice("50", "Encoder.FPS.Frames50"),
            new LLocalizationChoice("30", "Encoder.FPS.Frames30"),
            new LLocalizationChoice("25", "Encoder.FPS.Frames25"),
            new LLocalizationChoice("24", "Encoder.FPS.Frames24"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));
        psVideoPixelCombo = PSComboBuild(lsExportSpecificEdit.LPresetPixelLayout,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("yuv420p", "Encoder.Pixel.Yuv420"),
            new LLocalizationChoice("yuv422p", "Encoder.Pixel.Yuv422"),
            new LLocalizationChoice("yuv444p", "Encoder.Pixel.Yuv444"),
            new LLocalizationChoice("yuv420p10le", "Encoder.Pixel.Yuv420Ten"),
            new LLocalizationChoice("yuv422p10le", "Encoder.Pixel.Yuv422Ten"),
            new LLocalizationChoice("yuv444p10le", "Encoder.Pixel.Yuv444Ten"));
        psAudioEncoderCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudioEncoder,
            new LLocalizationChoice("AAC", "Encoder.AudioCodec.AAC"),
            new LLocalizationChoice("MP3 / libmp3lame", "Encoder.AudioCodec.MP3"),
            new LLocalizationChoice("Opus / libopus", "Encoder.AudioCodec.Opus"),
            new LLocalizationChoice("FLAC", "Encoder.AudioCodec.FLAC"),
            new LLocalizationChoice("PCM 16-bit / pcm_s16le", "Encoder.AudioCodec.PCM16"),
            new LLocalizationChoice("PCM 24-bit / pcm_s24le", "Encoder.AudioCodec.PCM24"));
        psAudioBitrateCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudioBitrate,
            new LLocalizationChoice("96k", "Encoder.Bitrate.Kbps96"),
            new LLocalizationChoice("128k", "Encoder.Bitrate.Kbps128"),
            new LLocalizationChoice("160k", "Encoder.Bitrate.Kbps160"),
            new LLocalizationChoice("192k", "Encoder.Bitrate.Kbps192"),
            new LLocalizationChoice("256k", "Encoder.Bitrate.Kbps256"),
            new LLocalizationChoice("320k", "Encoder.Bitrate.Kbps320"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));
        psAudioSampleCombo = PSComboBuild(lsExportSpecificEdit.LPresetSampleRate,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("44100", "Encoder.Sample.Hertz44100"),
            new LLocalizationChoice("48000", "Encoder.Sample.Hertz48000"),
            new LLocalizationChoice("88200", "Encoder.Sample.Hertz88200"),
            new LLocalizationChoice("96000", "Encoder.Sample.Hertz96000"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));
        psAudioChannelCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudioChannels,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("Mono", "Encoder.Value.Mono"),
            new LLocalizationChoice("Stereo", "Encoder.Value.Stereo"),
            new LLocalizationChoice("5.1", "Encoder.Channel.Surround"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));

        Title = LLocalization.LLocalizationTextRead("Encoder.Window.Title");
        Width = PSEncoderWidthDefault;
        Height = PSEncoderHeightDefault;
        MinWidth = PSEncoderWidthMinimum;
        MinHeight = PSEncoderHeightMinimum;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSFieldFontSize;
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
        lsExportSpecificEdit.LPresetDisplay = string.IsNullOrWhiteSpace(psNameBox.PTokenText) ? "{OriginalName}_export" : psNameBox.PTokenText.Trim();
        lsExportSpecificEdit.LPresetContainer = PSComboTextRead(psOutputContainerCombo);
        lsExportSpecificEdit.LPresetExportMode = PSComboTextRead(psModeCombo);
        lsExportSpecificEdit.LPresetVideoStream = PSComboTextRead(psVideoStreamCombo);
        lsExportSpecificEdit.LPresetAudioStream = PSComboTextRead(psAudioStreamCombo);
        lsExportSpecificEdit.LPresetVideoMode = PSComboTextRead(psVideoModeCombo);
        lsExportSpecificEdit.LPresetAudioMode = PSComboTextRead(psAudioModeCombo);
        lsExportSpecificEdit.LPresetVideoEncoder = PSComboTextRead(psVideoEncoderCombo);
        lsExportSpecificEdit.LPresetRateControl = PSComboTextRead(psVideoRateCombo);
        lsExportSpecificEdit.LPresetVideoQuality = psVideoQualityBox?.Text.Trim() ?? string.Empty;
        lsExportSpecificEdit.LPresetSpeedPreset = psVideoSpeedCombo is null ? string.Empty : PSComboTextRead(psVideoSpeedCombo);
        lsExportSpecificEdit.LPresetLocation = PSComboTextRead(psLocationCombo);
        lsExportSpecificEdit.LPresetLocationFolder = PSLocationNamedCheck(lsExportSpecificEdit.LPresetLocation)
            ? psLocationFolderBox.Text.Trim()
            : psEncoderFolderPath ?? string.Empty;
        lsExportSpecificEdit.LPresetVideoSize = PSVideoSizeRead(PSComboTextRead(psVideoSizeCombo));
        lsExportSpecificEdit.LPresetSizeReactive = psVideoReactiveBox.IsChecked == true;
        lsExportSpecificEdit.LPresetVideoFps = PSComboTextRead(psVideoFpsCombo);
        lsExportSpecificEdit.LPresetPixelLayout = PSComboTextRead(psVideoPixelCombo);
        lsExportSpecificEdit.LPresetVideoExtras = psVideoExtraCombos.ToDictionary(
            pExtra => pExtra.Key, pExtra => PSComboTextRead(pExtra.Value), StringComparer.Ordinal);
        lsExportSpecificEdit.LPresetAudioEncoder = PSComboTextRead(psAudioEncoderCombo);
        lsExportSpecificEdit.LPresetAudioBitrate = PSComboTextRead(psAudioBitrateCombo);
        lsExportSpecificEdit.LPresetSampleRate = PSComboTextRead(psAudioSampleCombo);
        lsExportSpecificEdit.LPresetAudioChannels = PSComboTextRead(psAudioChannelCombo);
        lsExportSpecificSource.LPresetCopy(lsExportSpecificEdit);
        psEncoderSummary();
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
