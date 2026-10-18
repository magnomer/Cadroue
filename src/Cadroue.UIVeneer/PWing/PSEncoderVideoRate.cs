using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoderVideoRate
{
    private readonly QSEncoderVideo lsEncoderVideo;
    private readonly LPreset lsEncoderDraft;
    private readonly Dictionary<string, ComboBox> psVideoExtraCombos = new(StringComparer.Ordinal);
    private TextBox? psVideoQualityBox;
    private Slider? psVideoSpeedSlider;
    private IReadOnlyList<LCapabilityChoice>? psVideoSpeedChoices;

    internal PSEncoderVideoRate(QSEncoderVideo lsVideo, LPreset lsDraft)
    {
        lsEncoderVideo = lsVideo;
        lsEncoderDraft = lsDraft;
    }

    internal StackPanel PSVideoRowsPanel { get; } = new();

    internal string PSVideoQualityRead() => PSEncoderValue.PSEncoderTrimRead(psVideoQualityBox);

    internal Dictionary<string, string> PSVideoExtrasRead() => PSEncoderValue.PSEncoderExtrasRead(psVideoExtraCombos);

    internal void PSVideoRowsRebuild()
    {
        PSVideoRowsClear();
        PSVideoRowsReset();
        LCapabilityCodec pCodec = lsEncoderVideo.QSEncoderVideoCodec;
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(lsEncoderVideo.QSEncoderVideoRate);
        bool pModeStored = PSVideoStoredCheck(pMode);
        PSVideoQualityBuild(pMode, pModeStored);
        PSVideoCautionBuild(pCodec);
        PSVideoSpeedBuild(pCodec, pModeStored);
        PSVideoExtraBuild(pCodec, pMode);
    }

    private void PSVideoRowsClear() => PSVideoRowsPanel.Children.Clear();

    private void PSVideoRowsReset()
    {
        psVideoQualityBox = null;
        psVideoSpeedSlider = null;
        psVideoSpeedChoices = null;
        psVideoExtraCombos.Clear();
    }

    private bool PSVideoStoredCheck(LCapabilityMode pMode) =>
        string.Equals(
            pMode.LCapabilityModeLabel,
            lsEncoderDraft.LPresetVideo.LPresetRateControl,
            StringComparison.Ordinal);

    private void PSVideoCautionBuild(LCapabilityCodec pCodec)
    {
        if (!string.IsNullOrWhiteSpace(pCodec.LCapabilityNotice))
        {
            PSVideoRowsPanel.Children.Add(PSNotice.PSNoticeBuild(pCodec.LCapabilityNotice));
        }
    }

    private void PSVideoQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.LCapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pText = PSVideoQualityResolve(pQuality, pModeStored);
        PSVideoEntryBuild(pQuality, pText);
    }

    private string PSVideoQualityResolve(LCapabilityQuality pQuality, bool pModeStored)
    {
        string pStored = lsEncoderDraft.LPresetVideo.LPresetQuality;
        return pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.LCapabilityQualityDefault;
    }

    private void PSVideoEntryBuild(LCapabilityQuality pQuality, string pText)
    {
        psVideoQualityBox = PSEntry.PSEntryBuild(pText, 120);
        if (pQuality.LCapabilityQualityMinimum is double pMinimum
            && pQuality.LCapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFader.PSFaderBitrateBuild(pMinimum, pMaximum, pText, psVideoQualityBox)
                : PSFader.PSFaderQualityBuild(
                    pMinimum,
                    pMaximum,
                    pQuality.LCapabilityQualityStep,
                    pText,
                    psVideoQualityBox,
                    pQuality.LCapabilityQualityAscending);
            PSVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pQuality.LCapabilityQualityLabel, pSliderRow));
        }
        else
        {
            PSVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pQuality.LCapabilityQualityLabel, psVideoQualityBox));
        }

        PSVideoRowsPanel.Children.Add(PSNotice.PSNoticeBuild(PSVideoRangeFormat(pQuality)));
    }

    private static string PSVideoRangeFormat(LCapabilityQuality pQuality)
    {
        string pRange = pQuality.LCapabilityQualityRange;
        return string.IsNullOrEmpty(pRange)
            ? QLocalizationCatalog.QLocalizationFormat("Encoder.Video.FFmpegOption", pQuality.LCapabilityQualityOption)
            : QLocalizationCatalog.QLocalizationFormat(
                "Encoder.Video.FFmpegOptionRange",
                pQuality.LCapabilityQualityOption,
                pRange);
    }

    private void PSVideoSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.LCapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pSelected = PSVideoSpeedResolve(pSpeed, pModeStored);
        IReadOnlyList<LCapabilityChoice> pChoices = pSpeed.LCapabilitySpeedValues;
        PSVideoChoicesSet(pChoices);
        int pIndex = PSVideoIndexFind(pChoices, pSelected);
        Slider pSlider = PSVideoSliderBuild(pChoices, pIndex);
        PSVideoSliderSet(pSlider);
        TextBlock pText = PSVideoLabelBuild(pChoices[pIndex].LCapabilityChoiceLabel);
        PSVideoSliderAttach(pSlider, pText, pChoices);
        PSVideoRowsPanel.Children.Add(
            PSField.PSFieldBuild(pSpeed.LCapabilitySpeedLabel, PSFader.PSFaderRowBuild(pSlider, pText)));
    }

    private string PSVideoSpeedResolve(LCapabilitySpeed pSpeed, bool pModeStored)
    {
        string pStored = lsEncoderDraft.LPresetVideo.LPresetSpeedPreset;
        return pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.LCapabilitySpeedDefault;
    }

    private void PSVideoChoicesSet(IReadOnlyList<LCapabilityChoice> pChoices) => psVideoSpeedChoices = pChoices;

    private static int PSVideoIndexFind(IReadOnlyList<LCapabilityChoice> pChoices, string pSelected)
    {
        for (int pAt = 0; pAt < pChoices.Count; pAt++)
        {
            if (string.Equals(pChoices[pAt].LCapabilityChoiceValue, pSelected, StringComparison.Ordinal))
            {
                return pAt;
            }
        }

        return 0;
    }

    private static Slider PSVideoSliderBuild(IReadOnlyList<LCapabilityChoice> pChoices, int pIndex)
    {
        int pLast = pChoices.Count - 1;
        return PSFader.PSFaderCreate(0, pLast, pLast - pIndex);
    }

    private void PSVideoSliderSet(Slider pSlider) => psVideoSpeedSlider = pSlider;

    private static TextBlock PSVideoLabelBuild(string pLabel) => new()
    {
        Text = pLabel,
        Foreground = PSField.PSFieldText,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static void PSVideoSliderAttach(Slider pSlider, TextBlock pText, IReadOnlyList<LCapabilityChoice> pChoices)
    {
        int pLast = pChoices.Count - 1;
        pSlider.ValueChanged += (_, _) =>
            pText.Text = pChoices[PSVideoIndexResolve(pSlider.Value, pLast)].LCapabilityChoiceLabel;
    }

    private static int PSVideoIndexResolve(double pValue, int pLast) =>
        pLast - Math.Clamp((int)Math.Round(pValue), 0, pLast);

    internal string PSVideoSpeedRead()
    {
        if (psVideoSpeedSlider is null || psVideoSpeedChoices is null || psVideoSpeedChoices.Count == 0)
        {
            return string.Empty;
        }

        int pLast = psVideoSpeedChoices.Count - 1;
        int pAt = pLast - Math.Clamp((int)Math.Round(psVideoSpeedSlider.Value), 0, pLast);
        return psVideoSpeedChoices[pAt].LCapabilityChoiceValue;
    }

    private void PSVideoExtraBuild(LCapabilityCodec pCodec, LCapabilityMode pMode)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            if (pMode.LCapabilityConflictCheck(pExtra.LCapabilityExtraOption))
            {
                continue;
            }

            string pSelected = PSVideoExtraResolve(pExtra);
            ComboBox pCombo = PSCombo.PSComboBuild(
                pSelected, PSEncoderValue.PSEncoderChoicesRead(pExtra.LCapabilityExtraValues));
            PSVideoExtraSet(pExtra, pCombo);
            PSVideoRowsPanel.Children.Add(PSField.PSFieldBuild(pExtra.LCapabilityExtraLabel, pCombo));
        }
    }

    private string PSVideoExtraResolve(LCapabilityExtra pExtra) =>
        lsEncoderDraft.LPresetVideo.LPresetExtras.TryGetValue(
                pExtra.LCapabilityExtraOption,
                out string? pStored)
            && pExtra.LCapabilityExtraValues.Any(pChoice => string.Equals(
                pChoice.LCapabilityChoiceValue,
                pStored,
                StringComparison.Ordinal))
            ? pStored
            : pExtra.LCapabilityExtraDefault;

    private void PSVideoExtraSet(LCapabilityExtra pExtra, ComboBox pCombo) =>
        psVideoExtraCombos[pExtra.LCapabilityExtraOption] = pCombo;
}
