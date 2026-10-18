using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private void PSAudioRowsRebuild()
    {
        PSAudioRowsClear();
        LCapabilityCodec pCodec = lsEncoder.QSEncoderAudio.QSEncoderAudioCodec;
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(lsEncoder.QSEncoderAudio.QSEncoderAudioRate);
        bool pModeStored = PSAudioModeCheck(pMode);

        PSAudioQualityBuild(pMode, pModeStored);
        PSAudioSpeedBuild(pCodec, pModeStored);
        PSAudioExtraBuild(pCodec);

        if (!string.IsNullOrWhiteSpace(pCodec.LCapabilityNotice))
        {
            PSAudioRowAdd(PSNotice.PSNoticeBuild(pCodec.LCapabilityNotice));
        }
    }

    private void PSAudioRowsClear()
    {
        psAudioRowsPanel.Children.Clear();
        psAudioQualityBox = null;
        psAudioSpeedCombo = null;
        psAudioExtraCombos.Clear();
    }

    private bool PSAudioModeCheck(LCapabilityMode pMode) => string.Equals(
        pMode.LCapabilityModeLabel,
        lsExportSpecificEdit.LPresetAudio.LPresetRateControl,
        StringComparison.Ordinal);

    private void PSAudioRowAdd(UIElement pRow) => psAudioRowsPanel.Children.Add(pRow);

    private static string PSAudioStoredResolve(bool pModeStored, string pStored, string pDefault) =>
        pModeStored && !string.IsNullOrWhiteSpace(pStored) ? pStored : pDefault;

    private void PSAudioQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.LCapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pText = PSAudioStoredResolve(
            pModeStored, lsExportSpecificEdit.LPresetAudio.LPresetQuality, pQuality.LCapabilityQualityDefault);
        TextBox pBox = PSAudioEntryBuild(pText);
        if (pQuality.LCapabilityQualityMinimum is double pMinimum
            && pQuality.LCapabilityQualityMaximum is double pMaximum)
        {
            PSAudioRowAdd(PSField.PSFieldBuild(
                pQuality.LCapabilityQualityLabel, PSAudioSliderBuild(pQuality, pMinimum, pMaximum, pText, pBox)));
        }
        else
        {
            PSAudioRowAdd(PSField.PSFieldBuild(pQuality.LCapabilityQualityLabel, pBox));
        }

        PSAudioRowAdd(PSNotice.PSNoticeBuild(PSAudioOptionFormat(pQuality)));
    }

    private TextBox PSAudioEntryBuild(string pText)
    {
        TextBox pBox = PSEntry.PSEntryBuild(pText, 120);
        psAudioQualityBox = pBox;
        return pBox;
    }

    private static UIElement PSAudioSliderBuild(
        LCapabilityQuality pQuality, double pMinimum, double pMaximum, string pText, TextBox pBox) =>
        pQuality.LCapabilityQualityBitrate
            ? PSFader.PSFaderBitrateBuild(pMinimum, pMaximum, pText, pBox)
            : PSFader.PSFaderQualityBuild(
                pMinimum,
                pMaximum,
                pQuality.LCapabilityQualityStep,
                pText,
                pBox,
                pQuality.LCapabilityQualityAscending);

    private static string PSAudioOptionFormat(LCapabilityQuality pQuality)
    {
        string pRange = pQuality.LCapabilityQualityRange;
        return string.IsNullOrEmpty(pRange)
            ? QLocalizationCatalog.QLocalizationFormat("Encoder.Audio.FFmpegOption", pQuality.LCapabilityQualityOption)
            : QLocalizationCatalog.QLocalizationFormat(
                "Encoder.Audio.FFmpegOptionRange",
                pQuality.LCapabilityQualityOption,
                pRange);
    }

    private void PSAudioSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.LCapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pSelected = PSAudioStoredResolve(
            pModeStored, lsExportSpecificEdit.LPresetAudio.LPresetSpeed, pSpeed.LCapabilitySpeedDefault);
        ComboBox pCombo = PSAudioPickerBuild(pSelected, pSpeed.LCapabilitySpeedValues);
        PSAudioRowAdd(PSField.PSFieldBuild(pSpeed.LCapabilitySpeedLabel, pCombo));
    }

    private ComboBox PSAudioPickerBuild(string pSelected, IReadOnlyList<LCapabilityChoice> pValues)
    {
        ComboBox pCombo = PSCombo.PSComboBuild(pSelected, PSEncoderChoicesRead(pValues));
        psAudioSpeedCombo = pCombo;
        return pCombo;
    }

    private void PSAudioExtraBuild(LCapabilityCodec pCodec)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            string pSelected = PSAudioExtraResolve(pExtra);
            ComboBox pCombo = PSAudioChoiceBuild(pExtra, pSelected);
            PSAudioRowAdd(PSField.PSFieldBuild(pExtra.LCapabilityExtraLabel, pCombo));
        }
    }

    private string PSAudioExtraResolve(LCapabilityExtra pExtra) =>
        lsExportSpecificEdit.LPresetAudio.LPresetExtras.TryGetValue(
                pExtra.LCapabilityExtraOption,
                out string? pStored)
            && pExtra.LCapabilityExtraValues.Any(pChoice => string.Equals(
                pChoice.LCapabilityChoiceValue,
                pStored,
                StringComparison.Ordinal))
            ? pStored
            : pExtra.LCapabilityExtraDefault;

    private ComboBox PSAudioChoiceBuild(LCapabilityExtra pExtra, string pSelected)
    {
        ComboBox pCombo = PSCombo.PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.LCapabilityExtraValues));
        psAudioExtraCombos[pExtra.LCapabilityExtraOption] = pCombo;
        return pCombo;
    }
}
