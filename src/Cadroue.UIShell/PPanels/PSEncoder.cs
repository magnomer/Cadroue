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
using Cadroue.Application;

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
    private readonly ComboBox psOutputExtensionCombo;
    private readonly ComboBox psOutputCollisionCombo;
    private readonly TextBox psOutputSuffixBox;
    private UIElement? psOutputSuffixRow;
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
    private readonly TextBox psVideoFpsCustom;
    private UIElement? psVideoFpsRow;
    private readonly ComboBox psVideoPixelCombo;
    private readonly ComboBox psAudioEncoderCombo;
    private readonly ComboBox psAudioRateCombo;
    private readonly ComboBox psAudioSampleCombo;
    private readonly TextBox psAudioSampleCustom;
    private UIElement? psAudioSampleRow;
    private readonly ComboBox psAudioChannelCombo;
    private readonly TextBox psAudioChannelCustom;
    private UIElement? psAudioChannelRow;
    private readonly StackPanel psVideoRowsPanel;
    private readonly StackPanel psAudioRowsPanel;
    private readonly StackPanel psVideoEncodePanel;
    private readonly StackPanel psAudioEncodePanel;
    private readonly TextBlock psVideoNotice;
    private readonly TextBlock psAudioNotice;
    private readonly TextBlock psVideoEncoderNotice;
    private readonly TextBlock psAudioEncoderNotice;
    private readonly Dictionary<string, ComboBox> psVideoExtraCombos;
    private readonly Dictionary<string, ComboBox> psAudioExtraCombos;
    private TextBox? psVideoQualityBox;
    private ComboBox? psVideoSpeedCombo;
    private bool psVideoRowsBusy;
    private TextBox? psAudioQualityBox;
    private ComboBox? psAudioSpeedCombo;
    private bool psAudioRowsBusy;
    private IReadOnlyList<PSVerdictRow> psCodecResults = Array.Empty<PSVerdictRow>();
    private IReadOnlyList<PSVerdictRow> psAudioResults = Array.Empty<PSVerdictRow>();
    private string? psEncoderFolderPath;
    private readonly PSGrabber psEncoderGrabber;

    private static readonly Brush PLineBrush = PSField.PSFieldLine;
    private static readonly Brush PSEncoderTextBrush = PSField.PSFieldText;
    private static readonly Brush PSEncoderMutedBrush = PSField.PSFieldMuted;
    private static TextBlock PSEncoderErrorBuild() => new()
    {
        Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x2A, 0x2A)),
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };

    private static readonly string[] psVideoFpsTokens = ["Same as source", "60", "50", "30", "25", "24"];
    private static readonly string[] psAudioSampleTokens = ["Same as source", "44100", "48000", "88200", "96000"];
    private static readonly string[] psAudioChannelTokens = ["Same as source", "Mono", "Stereo", "5.1"];

    private static LLocalizationChoice[] PSEncoderChoicesRead(IReadOnlyList<LCapabilityChoice> pChoices) =>
        pChoices
            .Select(pChoice => new LLocalizationChoice(pChoice.CapabilityChoiceValue, string.Empty, pChoice.CapabilityChoiceLabel))
            .ToArray();

    private static string PSEncoderCustomResolve(string pValue, string[] pTokens) =>
        Array.IndexOf(pTokens, pValue) >= 0 || string.Equals(pValue, PSField.PSFieldCustomToken, StringComparison.Ordinal)
            ? string.Empty
            : pValue;

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
            new LLocalizationChoice("AVI", "Encoder.Container.AVI"),
            new LLocalizationChoice("MPEG-TS", "Encoder.Container.TS"),
            new LLocalizationChoice("FLV", "Encoder.Container.FLV"),
            new LLocalizationChoice("Ogg", "Encoder.Container.Ogg"));
        psOutputExtensionCombo = PSComboBuild(lsExportSpecificEdit.LPresetExtension, PSOutputExtensionRead(lsExportSpecificEdit.LPresetContainer));
        psOutputCollisionCombo = PSComboBuild(lsExportSpecificEdit.LPresetCollision,
            new LLocalizationChoice("Overwrite", "Encoder.Collision.Overwrite"),
            new LLocalizationChoice("Rename output", "Encoder.Collision.RenameOutput"),
            new LLocalizationChoice("Rename existing", "Encoder.Collision.RenameExisting"));
        psOutputSuffixBox = PSEntryBuild(lsExportSpecificEdit.LPresetCollisionSuffix, 220);
        psAudioStreamCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudio.LPresetStream,
            new LLocalizationChoice("Include first audio track", "Encoder.Stream.FirstAudio"),
            new LLocalizationChoice("Include all audio tracks", "Encoder.Stream.AllAudio"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psVideoModeCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideo.LPresetMode,
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Smart", "Encoder.Codec.Smart"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"));
        psAudioModeCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudio.LPresetMode,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));

        psVideoEncoderCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideo.LPresetEncoder, PSCodecItemsRead(lsExportSpecificEdit.LPresetContainer, lsExportSpecificEdit.LPresetVideo.LPresetEncoder));
        LCapabilityCodec pVideoCodec = LCapability.LCapabilityRead(PSCodecValueRead(PSComboTextRead(psVideoEncoderCombo)));
        psVideoRateCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideo.LPresetRateControl, pVideoCodec.LCapabilityModeLabels);
        psVideoRowsPanel = new StackPanel();
        psAudioRowsPanel = new StackPanel();
        psVideoEncodePanel = new StackPanel();
        psAudioEncodePanel = new StackPanel();
        psVideoNotice = PSAudioNoticeBuild();
        psAudioNotice = PSAudioNoticeBuild();
        psVideoEncoderNotice = PSEncoderErrorBuild();
        psAudioEncoderNotice = PSEncoderErrorBuild();
        psVideoExtraCombos = new Dictionary<string, ComboBox>(StringComparer.Ordinal);
        psAudioExtraCombos = new Dictionary<string, ComboBox>(StringComparer.Ordinal);

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
            PSVideoLabelRead(lsExportSpecificEdit.LPresetVideo.LPresetSize, lsExportSpecificEdit.LPresetVideo.LPresetSizeReactive),
            PSVideoChoicesRead(lsExportSpecificEdit.LPresetVideo.LPresetSizeReactive));
        psVideoReactiveBox = PSVideoReactiveBuild(lsExportSpecificEdit.LPresetVideo.LPresetSizeReactive);
        string[] psCustomParts = lsExportSpecificEdit.LPresetVideo.LPresetSize.Split(
            ['x', 'X', '×'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        psVideoCustomWidth = PSEntryBuild(psCustomParts.Length == 2 ? psCustomParts[0] : string.Empty, 110);
        psVideoCustomHeight = PSEntryBuild(psCustomParts.Length == 2 ? psCustomParts[1] : string.Empty, 110);
        psVideoFpsCombo = PSComboBuild(PSFieldCustomResolve(lsExportSpecificEdit.LPresetVideo.LPresetFps, psVideoFpsTokens),
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("60", "Encoder.FPS.Frames60"),
            new LLocalizationChoice("50", "Encoder.FPS.Frames50"),
            new LLocalizationChoice("30", "Encoder.FPS.Frames30"),
            new LLocalizationChoice("25", "Encoder.FPS.Frames25"),
            new LLocalizationChoice("24", "Encoder.FPS.Frames24"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));
        psVideoFpsCustom = PSEntryBuild(PSEncoderCustomResolve(lsExportSpecificEdit.LPresetVideo.LPresetFps, psVideoFpsTokens), 120);
        psVideoPixelCombo = PSComboBuild(lsExportSpecificEdit.LPresetVideo.LPresetPixelLayout,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("yuv420p", "Encoder.Pixel.Yuv420"),
            new LLocalizationChoice("yuv422p", "Encoder.Pixel.Yuv422"),
            new LLocalizationChoice("yuv444p", "Encoder.Pixel.Yuv444"),
            new LLocalizationChoice("yuv420p10le", "Encoder.Pixel.Yuv420Ten"),
            new LLocalizationChoice("yuv422p10le", "Encoder.Pixel.Yuv422Ten"),
            new LLocalizationChoice("yuv444p10le", "Encoder.Pixel.Yuv444Ten"));
        psAudioEncoderCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudio.LPresetEncoder, PSAudioItemsRead(lsExportSpecificEdit.LPresetContainer, lsExportSpecificEdit.LPresetAudio.LPresetEncoder));
        LCapabilityCodec pAudioCodec = LCapability.LCapabilityAudioRead(LCapability.LCapabilityNameRead(PSComboTextRead(psAudioEncoderCombo)));
        psAudioRateCombo = PSComboBuild(lsExportSpecificEdit.LPresetAudio.LPresetRateControl, pAudioCodec.LCapabilityModeLabels);
        psAudioSampleCombo = PSComboBuild(PSFieldCustomResolve(lsExportSpecificEdit.LPresetAudio.LPresetSampleRate, psAudioSampleTokens),
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("44100", "Encoder.Sample.Hertz44100"),
            new LLocalizationChoice("48000", "Encoder.Sample.Hertz48000"),
            new LLocalizationChoice("88200", "Encoder.Sample.Hertz88200"),
            new LLocalizationChoice("96000", "Encoder.Sample.Hertz96000"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));
        psAudioSampleCustom = PSEntryBuild(PSEncoderCustomResolve(lsExportSpecificEdit.LPresetAudio.LPresetSampleRate, psAudioSampleTokens), 120);
        psAudioChannelCombo = PSComboBuild(PSFieldCustomResolve(lsExportSpecificEdit.LPresetAudio.LPresetChannels, psAudioChannelTokens),
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("Mono", "Encoder.Value.Mono"),
            new LLocalizationChoice("Stereo", "Encoder.Value.Stereo"),
            new LLocalizationChoice("5.1", "Encoder.Channel.Surround"),
            new LLocalizationChoice("Custom", "Encoder.Value.Custom"));
        psAudioChannelCustom = PSEntryBuild(PSEncoderCustomResolve(lsExportSpecificEdit.LPresetAudio.LPresetChannels, psAudioChannelTokens), 120);

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
        PSCodecProbeDefer();
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
        lsExportSpecificEdit.LPresetExtension = PSComboTextRead(psOutputExtensionCombo);
        lsExportSpecificEdit.LPresetCollision = PSComboTextRead(psOutputCollisionCombo);
        lsExportSpecificEdit.LPresetCollisionSuffix = psOutputSuffixBox.Text.Trim();
        lsExportSpecificEdit.LPresetVideo.LPresetStream = "Include";
        lsExportSpecificEdit.LPresetAudio.LPresetStream = PSComboTextRead(psAudioStreamCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetMode = PSComboTextRead(psVideoModeCombo);
        lsExportSpecificEdit.LPresetAudio.LPresetMode = PSComboTextRead(psAudioModeCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetEncoder = PSComboTextRead(psVideoEncoderCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetRateControl = PSComboTextRead(psVideoRateCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetQuality = psVideoQualityBox?.Text.Trim() ?? string.Empty;
        lsExportSpecificEdit.LPresetVideo.LPresetSpeedPreset = psVideoSpeedCombo is null ? string.Empty : PSComboTextRead(psVideoSpeedCombo);
        lsExportSpecificEdit.LPresetLocation = PSComboTextRead(psLocationCombo);
        lsExportSpecificEdit.LPresetLocationFolder = PSLocationNamedCheck(lsExportSpecificEdit.LPresetLocation)
            ? psLocationFolderBox.Text.Trim()
            : psEncoderFolderPath ?? string.Empty;
        lsExportSpecificEdit.LPresetVideo.LPresetSize = PSVideoSizeRead(PSComboTextRead(psVideoSizeCombo));
        lsExportSpecificEdit.LPresetVideo.LPresetSizeReactive = psVideoReactiveBox.IsChecked == true;
        lsExportSpecificEdit.LPresetVideo.LPresetFps = PSFieldCustomRead(psVideoFpsCombo, psVideoFpsCustom, "Same as source");
        lsExportSpecificEdit.LPresetVideo.LPresetPixelLayout = PSComboTextRead(psVideoPixelCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetExtras = psVideoExtraCombos.ToDictionary(
            pExtra => pExtra.Key, pExtra => PSComboTextRead(pExtra.Value), StringComparer.Ordinal);
        lsExportSpecificEdit.LPresetAudio.LPresetEncoder = PSComboTextRead(psAudioEncoderCombo);
        lsExportSpecificEdit.LPresetAudio.LPresetRateControl = PSComboTextRead(psAudioRateCombo);
        lsExportSpecificEdit.LPresetAudio.LPresetQuality = psAudioQualityBox?.Text.Trim() ?? string.Empty;
        lsExportSpecificEdit.LPresetAudio.LPresetSpeed = psAudioSpeedCombo is null ? string.Empty : PSComboTextRead(psAudioSpeedCombo);
        lsExportSpecificEdit.LPresetAudio.LPresetExtras = psAudioExtraCombos.ToDictionary(
            pExtra => pExtra.Key, pExtra => PSComboTextRead(pExtra.Value), StringComparer.Ordinal);
        lsExportSpecificEdit.LPresetAudio.LPresetSampleRate = PSFieldCustomRead(psAudioSampleCombo, psAudioSampleCustom, "Same as source");
        lsExportSpecificEdit.LPresetAudio.LPresetChannels = PSFieldCustomRead(psAudioChannelCombo, psAudioChannelCustom, "Same as source");
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
