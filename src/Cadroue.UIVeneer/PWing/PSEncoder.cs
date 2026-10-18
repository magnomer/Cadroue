using Cadroue.Core;
using Cadroue.UIVeneer.PHouse;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder : Window
{
    internal const string PSEncoderPlacementKey = "Encoder";

    internal const double PSEncoderWidthDefault = 820;
    internal const double PSEncoderHeightDefault = 770;
    internal const double PSEncoderWidthMinimum = 680;
    internal const double PSEncoderHeightMinimum = 770;

    private readonly QSEncoder lsEncoder;
    private readonly System.Action psEncoderSummary;
    private readonly RichTextBox psNameBox = new() { MinWidth = 320 };

    private LPreset lsExportSpecificEdit => lsEncoder.QSEncoderDraft;
    private readonly ListBox psOutputContainerCombo;
    private readonly ComboBox psOutputExtensionCombo;
    private readonly ComboBox psOutputCollisionCombo;
    private readonly TextBox psOutputSuffixBox;
    private UIElement? psOutputSuffixRow;
    private TextBlock? psOutputSuffixLabel;
    private readonly ComboBox psAudioStreamCombo;
    private readonly Border psVideoMode;
    private readonly Border psAudioMode;
    private readonly ComboBox psVideoEncoderCombo;
    private readonly ComboBox psVideoRateCombo;
    private readonly Border psLocationMode;
    private readonly TextBox psLocationFolderBox;
    private UIElement? psLocationFolderRow;
    private TextBlock? psLocationFolderLabel;
    private Button? psLocationBrowse;
    private TextBlock? psLocationStatus;
    private readonly CheckBox psVideoReactiveBox;
    private Slider? psVideoResolutionSlider;
    private TextBlock? psVideoResolutionValue;
    private TextBox? psVideoWidthBox;
    private TextBox? psVideoHeightBox;
    private Slider? psVideoWidthSlider;
    private Slider? psVideoHeightSlider;
    private TextBlock? psVideoWidthLabel;
    private TextBlock? psVideoHeightLabel;
    private readonly TextBox psVideoFpsCustom;
    private UIElement? psVideoFpsNotice;
    private UIElement? psVideoSizeNotice;
    private readonly ComboBox psVideoPixelCombo;
    private readonly ComboBox psAudioEncoderCombo;
    private readonly ComboBox psAudioRateCombo;
    private readonly StackPanel psAudioSamplePanel = new();
    private TextBox? psAudioSampleReadout;
    private readonly StackPanel psAudioChannelPanel = new();
    private Slider? psAudioChannelSlider;
    private TextBox? psAudioChannelReadout;
    private IReadOnlyList<string> psAudioChannelLayouts = Array.Empty<string>();
    private readonly StackPanel psVideoRowsPanel = new();
    private readonly StackPanel psAudioRowsPanel = new();
    private readonly StackPanel psVideoEncodePanel = new();
    private readonly StackPanel psAudioEncodePanel = new();
    private readonly TextBlock psVideoNotice;
    private readonly TextBlock psAudioNotice;
    private readonly TextBlock psVideoEncoderNotice;
    private readonly TextBlock psAudioEncoderNotice;
    private readonly Dictionary<string, ComboBox> psVideoExtraCombos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ComboBox> psAudioExtraCombos = new(StringComparer.Ordinal);
    private TextBox? psVideoQualityBox;
    private Slider? psVideoSpeedSlider;
    private IReadOnlyList<LCapabilityChoice>? psVideoSpeedChoices;
    private TextBox? psAudioQualityBox;
    private ComboBox? psAudioSpeedCombo;
    private readonly PSGrabber psEncoderGrabber;

    private static readonly Brush PLineBrush = PSField.PSFieldLine;
    private static readonly Brush PSEncoderTextBrush = PSField.PSFieldText;
    private static readonly Brush PSEncoderMutedBrush = PSField.PSFieldMuted;
    private static TextBlock PSEncoderErrorBuild() => new()
    {
        Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x2A, 0x2A)),
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };

    private static LLocalizationChoice[] PSEncoderChoicesRead(IReadOnlyList<LCapabilityChoice> pChoices) =>
        pChoices
            .Select(pChoice => new LLocalizationChoice(
                pChoice.LCapabilityChoiceValue,
                string.Empty,
                pChoice.LCapabilityChoiceLabel))
            .ToArray();

    public PSEncoder(QSEncoder lsEncoderOwner, System.Action pRefresh)
    {
        lsEncoder = lsEncoderOwner;
        PSEncoderAttach();
        psEncoderSummary = pRefresh;
        QTokenControl.QTokenControlAttach(
            psNameBox, lsEncoder.QSEncoderTokenCreate(), lsExportSpecificEdit.LPresetDisplay);
        psOutputContainerCombo = PSField.PSListBuild(lsExportSpecificEdit.LPresetContainer,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("MP4", "Encoder.Container.MP4"),
            new LLocalizationChoice("Matroska", "Encoder.Container.Matroska"),
            new LLocalizationChoice("MOV", "Encoder.Container.MOV"),
            new LLocalizationChoice("WebM", "Encoder.Container.WebM"),
            new LLocalizationChoice("AVI", "Encoder.Container.AVI"),
            new LLocalizationChoice("MPEG-TS", "Encoder.Container.TS"),
            new LLocalizationChoice("FLV", "Encoder.Container.FLV"),
            new LLocalizationChoice("Ogg", "Encoder.Container.Ogg"));
        psOutputExtensionCombo = PSCombo.PSComboBuild(
            lsExportSpecificEdit.LPresetExtension,
            PSOutputExtensionRead(lsExportSpecificEdit.LPresetContainer));
        psOutputCollisionCombo = PSCombo.PSComboBuild(lsExportSpecificEdit.LPresetCollision,
            new LLocalizationChoice("Overwrite", "Encoder.Collision.Overwrite"),
            new LLocalizationChoice("Rename output", "Encoder.Collision.RenameOutput"),
            new LLocalizationChoice("Rename existing", "Encoder.Collision.RenameExisting"));
        psOutputSuffixBox = PSEntry.PSEntryBuild(
            lsExportSpecificEdit.LPresetSuffixRead(lsExportSpecificEdit.LPresetCollision),
            220);
        psAudioStreamCombo = PSCombo.PSComboBuild(lsExportSpecificEdit.LPresetAudio.LPresetStream,
            new LLocalizationChoice("Include first audio track", "Encoder.Stream.FirstAudio"),
            new LLocalizationChoice("Include all audio tracks", "Encoder.Stream.AllAudio"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));
        psVideoMode = PSField.PSModeBuild(lsExportSpecificEdit.LPresetVideo.LPresetMode, PSVideoScopeUpdate,
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Smart", "Encoder.Codec.Smart"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"));
        psAudioMode = PSField.PSModeBuild(lsExportSpecificEdit.LPresetAudio.LPresetMode, PSAudioScopeUpdate,
            new LLocalizationChoice("Copy", "Encoder.Codec.Copy"),
            new LLocalizationChoice("Encode", "Encoder.Codec.Encode"),
            new LLocalizationChoice("Exclude", "Encoder.Stream.Exclude"));

        psVideoEncoderCombo = PSCombo.PSComboBuild(
            lsEncoder.QSEncoderVideo.QSEncoderVideoEncoder,
            QSEncoderVideo.QSEncoderVideoRead(
                lsExportSpecificEdit.LPresetContainer, lsEncoder.QSEncoderVideo.QSEncoderVideoEncoder));
        PSEncoderChipApply(psVideoEncoderCombo);
        psVideoRateCombo = PSCombo.PSComboBuild(
            lsEncoder.QSEncoderVideo.QSEncoderVideoRate,
            lsEncoder.QSEncoderVideo.QSEncoderVideoCodec.LCapabilityModeLabels);
        psVideoNotice = PSAudioNoticeBuild();
        psAudioNotice = PSAudioNoticeBuild();
        psVideoEncoderNotice = PSEncoderErrorBuild();
        psAudioEncoderNotice = PSEncoderErrorBuild();

        psLocationMode = PSField.PSModeBuild(lsExportSpecificEdit.LPresetLocation, PSLocationModeUpdate,
            new LLocalizationChoice("Same as source", "Encoder.Location.SourceFolder"),
            new LLocalizationChoice("Subfolder", "Encoder.Location.Subfolder"),
            new LLocalizationChoice("Sibling", "Encoder.Location.Sibling"),
            new LLocalizationChoice("Custom location", "Encoder.Location.Custom"));
        psLocationFolderBox = PSEntry.PSEntryBuild(
            lsExportSpecificEdit.LPresetLocationRead(lsExportSpecificEdit.LPresetLocation),
            220);
        psVideoReactiveBox = PSVideoReactiveBuild(lsExportSpecificEdit.LPresetVideo.LPresetSizeReactive);
        psVideoFpsCustom = PSEntry.PSEntryBuild(PSVideoFpsFormat(lsExportSpecificEdit.LPresetVideo.LPresetFps), 120);
        psVideoPixelCombo = PSCombo.PSComboBuild(lsExportSpecificEdit.LPresetVideo.LPresetPixelLayout,
            new LLocalizationChoice("Auto", "Encoder.Codec.Auto"),
            new LLocalizationChoice("yuv420p", "Encoder.Pixel.Yuv420"),
            new LLocalizationChoice("yuv422p", "Encoder.Pixel.Yuv422"),
            new LLocalizationChoice("yuv444p", "Encoder.Pixel.Yuv444"),
            new LLocalizationChoice("yuv420p10le", "Encoder.Pixel.Yuv420Ten"),
            new LLocalizationChoice("yuv422p10le", "Encoder.Pixel.Yuv422Ten"),
            new LLocalizationChoice("yuv444p10le", "Encoder.Pixel.Yuv444Ten"));
        psAudioEncoderCombo = PSCombo.PSComboBuild(
            lsEncoder.QSEncoderAudio.QSEncoderAudioEncoder,
            QSEncoderAudio.QSEncoderAudioRead(
                lsExportSpecificEdit.LPresetContainer, lsEncoder.QSEncoderAudio.QSEncoderAudioEncoder));
        PSEncoderChipApply(psAudioEncoderCombo);
        psAudioRateCombo = PSCombo.PSComboBuild(
            lsEncoder.QSEncoderAudio.QSEncoderAudioRate,
            lsEncoder.QSEncoderAudio.QSEncoderAudioCodec.LCapabilityModeLabels);
        PSEncoderWindowApply();
        PSSubwindow.PSSubwindowApply(this);
        Content = PSEncoderBuild();
        PSGrabber.PSGrabberPlacementRestore(this, PSEncoderPlacementKey);
        psEncoderGrabber = new PSGrabber(this);
        psEncoderGrabber.PSGrabberAttach();
        PSCodecProbeDefer();
        PSEncoderCloseAttach();
    }

    private void PSEncoderAttach()
    {
        lsEncoder.QSEncoderVideo.QSEncoderVideoChanged += PSVideoRowsApply;
        lsEncoder.QSEncoderAudio.QSEncoderAudioChanged += PSAudioRowsApply;
        lsEncoder.QSEncoderHelpAttach(PSEncoderHelpHandle);
        lsEncoder.QSEncoderSize.QSEncoderSizeChanged += PSVideoSizeApply;
    }

    private static void PSEncoderChipApply(ComboBox pCombo)
    {
        pCombo.ItemTemplate = psEncoderChipTemplate;
        pCombo.HorizontalContentAlignment = HorizontalAlignment.Stretch;
    }

    private static string PSVideoFpsFormat(string pFps) => PSVideoSourceCheck(pFps) ? string.Empty : pFps.Trim();

    private void PSEncoderWindowApply()
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Encoder.Window.Title");
        Width = PSEncoderWidthDefault;
        Height = PSEncoderHeightDefault;
        MinWidth = PSEncoderWidthMinimum;
        MinHeight = PSEncoderHeightMinimum;
    }

    private void PSEncoderCloseAttach() => Closed += PSEncoderCloseHandle;

    private UIElement PSEncoderBuild() =>
        PSSubwindow.PSSubwindowBuild(this, PSSheetStripWidth, PSSheetControlBuild());

    private void PSEncoderApply()
    {
        PSEncoderDraftSet();
        lsEncoder.QSEncoderApply();
        psEncoderSummary();
    }

    private void PSEncoderDraftSet()
    {
        lsExportSpecificEdit.LPresetDisplay = QTokenControl.QTokenControlRead(psNameBox);
        lsExportSpecificEdit.LPresetContainer = PSCombo.PSComboTextRead(psOutputContainerCombo);
        lsExportSpecificEdit.LPresetExtension = PSCombo.PSComboTextRead(psOutputExtensionCombo);
        lsExportSpecificEdit.LPresetCollision = PSCombo.PSComboTextRead(psOutputCollisionCombo);
        lsExportSpecificEdit.LPresetSuffixSet(
            lsExportSpecificEdit.LPresetCollision, PSEncoderTrimRead(psOutputSuffixBox));
        lsExportSpecificEdit.LPresetAudio.LPresetStream = PSCombo.PSComboTextRead(psAudioStreamCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetMode = PSField.PSModeTextRead(psVideoMode);
        lsExportSpecificEdit.LPresetAudio.LPresetMode = PSField.PSModeTextRead(psAudioMode);
        lsExportSpecificEdit.LPresetVideo.LPresetQuality = PSEncoderTrimRead(psVideoQualityBox);
        lsExportSpecificEdit.LPresetVideo.LPresetSpeedPreset = PSVideoSpeedRead();
        lsExportSpecificEdit.LPresetLocation = PSField.PSModeTextRead(psLocationMode);
        lsExportSpecificEdit.LPresetLocationSet(
            lsExportSpecificEdit.LPresetLocation, PSEncoderTrimRead(psLocationFolderBox));
        lsExportSpecificEdit.LPresetVideo.LPresetSizeReactive = PSEncoderCheckRead(psVideoReactiveBox);
        lsExportSpecificEdit.LPresetVideo.LPresetFps = PSVideoFpsRead();
        lsExportSpecificEdit.LPresetVideo.LPresetPixelLayout = PSCombo.PSComboTextRead(psVideoPixelCombo);
        lsExportSpecificEdit.LPresetVideo.LPresetExtras = PSEncoderExtrasRead(psVideoExtraCombos);
        lsExportSpecificEdit.LPresetAudio.LPresetQuality = PSEncoderTrimRead(psAudioQualityBox);
        lsExportSpecificEdit.LPresetAudio.LPresetSpeed = PSAudioSpeedRead();
        lsExportSpecificEdit.LPresetAudio.LPresetExtras = PSEncoderExtrasRead(psAudioExtraCombos);
        lsExportSpecificEdit.LPresetAudio.LPresetSampleRate = PSAudioSampleRead();
        lsExportSpecificEdit.LPresetAudio.LPresetChannels = PSAudioChannelRead();
    }

    private static string PSEncoderTrimRead(TextBox? pBox) => pBox?.Text.Trim() ?? string.Empty;

    private static bool PSEncoderCheckRead(CheckBox pBox) => pBox.IsChecked == true;

    private static Dictionary<string, string> PSEncoderExtrasRead(Dictionary<string, ComboBox> pCombos) =>
        pCombos.ToDictionary(
            pExtra => pExtra.Key, pExtra => PSCombo.PSComboTextRead(pExtra.Value), StringComparer.Ordinal);

    private string PSAudioSpeedRead() =>
        psAudioSpeedCombo is null ? string.Empty : PSCombo.PSComboTextRead(psAudioSpeedCombo);

    private void PSEncoderHelpHandle() => Dispatcher.InvokeAsync(PSAudioRowsApply);

    private void PSEncoderCloseHandle(object? sender, System.EventArgs e)
    {
        lsEncoder.QSEncoderClose();
        PSGrabber.PSGrabberPlacementSave(this, PSEncoderPlacementKey);
        psEncoderGrabber.PSGrabberDetach();
        PSEncoderCloseDetach();
    }

    private void PSEncoderCloseDetach() => Closed -= PSEncoderCloseHandle;
}
