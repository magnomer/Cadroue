using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSEncoderAudioRate
{
    private readonly QSEncoderAudio lsEncoderAudio;
    private readonly LPreset lsEncoderDraft;
    private readonly Dictionary<string, ComboBox> psAudioExtraCombos = new(StringComparer.Ordinal);
    private TextBox? psAudioQualityBox;
    private ComboBox? psAudioSpeedCombo;

    internal PSEncoderAudioRate(QSEncoderAudio lsAudio, LPreset lsDraft)
    {
        lsEncoderAudio = lsAudio;
        lsEncoderDraft = lsDraft;
    }

    internal StackPanel PSAudioRowsPanel { get; } = new();

    internal string PSAudioQualityRead() => PSEncoderValue.PSEncoderTrimRead(psAudioQualityBox);

    internal string PSAudioSpeedRead() => QSEncoderAudio.QSEncoderAudioRead(psAudioSpeedCombo);

    internal Dictionary<string, string> PSAudioExtrasRead() => PSEncoderValue.PSEncoderExtrasRead(psAudioExtraCombos);

    internal void PSAudioRowsRebuild()
    {
        PSAudioRowsClear();
        QSEncoderAudioCodec pCodec = lsEncoderAudio.QSEncoderAudioRead();
        bool pModeStored = PSAudioModeCheck(pCodec.QSEncoderAudioMode);

        PSAudioQualityBuild(pCodec.QSEncoderAudioQuality, pModeStored);
        PSAudioSpeedBuild(pCodec.QSEncoderAudioSpeed, pModeStored);
        PSAudioExtraBuild(pCodec.QSEncoderAudioExtras);

        if (!string.IsNullOrWhiteSpace(pCodec.QSEncoderAudioCaution))
        {
            PSAudioRowAdd(PSNotice.PSNoticeBuild(pCodec.QSEncoderAudioCaution));
        }
    }

    private void PSAudioRowsClear()
    {
        PSAudioRowsPanel.Children.Clear();
        psAudioQualityBox = null;
        psAudioSpeedCombo = null;
        psAudioExtraCombos.Clear();
    }

    private bool PSAudioModeCheck(string pMode) => string.Equals(
        pMode,
        lsEncoderDraft.LPresetAudio.LPresetRateControl,
        StringComparison.Ordinal);

    private void PSAudioRowAdd(UIElement pRow) => PSAudioRowsPanel.Children.Add(pRow);

    private static string PSAudioStoredResolve(bool pModeStored, string pStored, string pDefault) =>
        pModeStored && !string.IsNullOrWhiteSpace(pStored) ? pStored : pDefault;

    private void PSAudioQualityBuild(QSCodecQuality? pQuality, bool pModeStored)
    {
        if (pQuality is null)
        {
            return;
        }

        string pText = PSAudioStoredResolve(
            pModeStored, lsEncoderDraft.LPresetAudio.LPresetQuality, pQuality.QSCodecQualityDefault);
        PSAudioRowAdd(PSAudioEntryBuild(pQuality, pText));
    }

    private PSFader PSAudioEntryBuild(QSCodecQuality pQuality, string pText)
    {
        var pFader = new PSFader();
        psAudioQualityBox = QSFader.QSFaderBuild(
            pFader,
            pQuality.QSCodecQualityLabel,
            pText,
            pQuality.QSCodecQualityMinimum,
            pQuality.QSCodecQualityMaximum,
            pQuality.QSCodecQualityStep,
            pQuality.QSCodecQualityAscending,
            pQuality.QSCodecQualityBitrate,
            pQuality.QSCodecQualityRange);
        return pFader;
    }

    private void PSAudioSpeedBuild(QSCodecSpeed? pSpeed, bool pModeStored)
    {
        if (pSpeed is null)
        {
            return;
        }

        string pSelected = PSAudioStoredResolve(
            pModeStored, lsEncoderDraft.LPresetAudio.LPresetSpeed, pSpeed.QSCodecSpeedDefault);
        ComboBox pCombo = PSAudioPickerBuild(pSelected, pSpeed.QSCodecSpeedValues);
        PSAudioRowAdd(PSField.PSFieldBuild(pSpeed.QSCodecSpeedLabel, pCombo));
    }

    private ComboBox PSAudioPickerBuild(string pSelected,
        IReadOnlyList<(string QSEncoderChoiceToken, string QSEncoderChoiceLabel)> pValues)
    {
        ComboBox pCombo = QSEncoderAudio.QSEncoderAudioBuild(
            pSelected, pValues, 260, PSField.PSFieldControlHeight);
        psAudioSpeedCombo = pCombo;
        return pCombo;
    }

    private void PSAudioExtraBuild(IReadOnlyList<(string QSCodecExtraLabel, string QSCodecExtraOption,
        string QSCodecExtraDefault, IReadOnlyList<(string QSEncoderChoiceToken,
        string QSEncoderChoiceLabel)> QSCodecExtraValues)> pExtras)
    {
        foreach (var pExtra in pExtras)
        {
            string pSelected = PSAudioExtraResolve(pExtra);
            ComboBox pCombo = PSAudioChoiceBuild(pExtra, pSelected);
            PSAudioRowAdd(PSField.PSFieldBuild(pExtra.QSCodecExtraLabel, pCombo));
        }
    }

    private string PSAudioExtraResolve((string QSCodecExtraLabel, string QSCodecExtraOption,
        string QSCodecExtraDefault, IReadOnlyList<(string QSEncoderChoiceToken,
        string QSEncoderChoiceLabel)> QSCodecExtraValues) pExtra) =>
        lsEncoderDraft.LPresetAudio.LPresetExtras.TryGetValue(
                pExtra.QSCodecExtraOption,
                out string? pStored)
            && pExtra.QSCodecExtraValues.Any(pChoice => string.Equals(
                pChoice.QSEncoderChoiceToken,
                pStored,
                StringComparison.Ordinal))
            ? pStored
            : pExtra.QSCodecExtraDefault;

    private ComboBox PSAudioChoiceBuild((string QSCodecExtraLabel, string QSCodecExtraOption,
        string QSCodecExtraDefault, IReadOnlyList<(string QSEncoderChoiceToken,
        string QSEncoderChoiceLabel)> QSCodecExtraValues) pExtra, string pSelected)
    {
        ComboBox pCombo = QSEncoderAudio.QSEncoderAudioBuild(
            pSelected, pExtra.QSCodecExtraValues, 260, PSField.PSFieldControlHeight);
        psAudioExtraCombos[pExtra.QSCodecExtraOption] = pCombo;
        return pCombo;
    }
}
