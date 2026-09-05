using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Infrastructure;

using static Cadroue.UIShell.PSCasement.PSField;
using static Cadroue.UIShell.PSCasement.PSCombo;
using static Cadroue.UIShell.PSCasement.PSEntry;
using static Cadroue.UIShell.PSCasement.PSNotice;
using static Cadroue.UIShell.PSCasement.PSFader;

namespace Cadroue.UIShell.PPanel;

internal sealed partial class PSEncoder
{
    private void PSAudioRowsRebuild()
    {
        if (psAudioRowsBusy)
        {
            return;
        }

        psAudioRowsPanel.Children.Clear();
        psAudioQualityBox = null;
        psAudioSpeedCombo = null;
        psAudioExtraCombos.Clear();

        LCapabilityCodec pCodec = PSAudioCapabilityRead();
        LCapabilityMode pMode = pCodec.LCapabilityModeFind(PSComboTextRead(psAudioRateCombo));
        bool pModeStored = string.Equals(pMode.LCapabilityModeLabel, lsExportSpecificEdit.LPresetAudio.LPresetRateControl, StringComparison.Ordinal);

        PSAudioQualityBuild(pMode, pModeStored);
        PSAudioSpeedBuild(pCodec, pModeStored);
        PSAudioExtraBuild(pCodec);

        if (!string.IsNullOrWhiteSpace(pCodec.LCapabilityNotice))
        {
            psAudioRowsPanel.Children.Add(PSNoticeBuild(pCodec.LCapabilityNotice));
        }
    }

    private void PSAudioQualityBuild(LCapabilityMode pMode, bool pModeStored)
    {
        if (pMode.LCapabilityModeQuality is not LCapabilityQuality pQuality)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetAudio.LPresetQuality;
        string pText = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pQuality.LCapabilityQualityDefault;

        psAudioQualityBox = PSEntryBuild(pText, 120);
        if (pQuality.LCapabilityQualityMinimum is double pMinimum && pQuality.LCapabilityQualityMaximum is double pMaximum)
        {
            UIElement pSliderRow = pQuality.LCapabilityQualityBitrate
                ? PSFaderBitrateBuild(pMinimum, pMaximum, pText, psAudioQualityBox)
                : PSFaderQualityBuild(pMinimum, pMaximum, pQuality.LCapabilityQualityStep, pText, psAudioQualityBox, pQuality.LCapabilityQualityAscending);
            psAudioRowsPanel.Children.Add(PSFieldBuild(pQuality.LCapabilityQualityLabel, pSliderRow));
        }
        else
        {
            psAudioRowsPanel.Children.Add(PSFieldBuild(pQuality.LCapabilityQualityLabel, psAudioQualityBox));
        }

        string pRange = pQuality.LCapabilityQualityRange;
        psAudioRowsPanel.Children.Add(PSNoticeBuild(string.IsNullOrEmpty(pRange)
            ? LLocalization.LLocalizationFormat("Encoder.Audio.FFmpegOption", pQuality.LCapabilityQualityOption)
            : LLocalization.LLocalizationFormat("Encoder.Audio.FFmpegOptionRange", pQuality.LCapabilityQualityOption, pRange)));
    }

    private void PSAudioSpeedBuild(LCapabilityCodec pCodec, bool pModeStored)
    {
        if (pCodec.LCapabilitySpeed is not LCapabilitySpeed pSpeed)
        {
            return;
        }

        string pStored = lsExportSpecificEdit.LPresetAudio.LPresetSpeed;
        string pSelected = pModeStored && !string.IsNullOrWhiteSpace(pStored)
            ? pStored
            : pSpeed.LCapabilitySpeedDefault;

        psAudioSpeedCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pSpeed.LCapabilitySpeedValues));
        psAudioRowsPanel.Children.Add(PSFieldBuild(pSpeed.LCapabilitySpeedLabel, psAudioSpeedCombo));
    }

    private void PSAudioExtraBuild(LCapabilityCodec pCodec)
    {
        foreach (LCapabilityExtra pExtra in pCodec.LCapabilityExtraList)
        {
            string pSelected = lsExportSpecificEdit.LPresetAudio.LPresetExtras.TryGetValue(pExtra.LCapabilityExtraOption, out string? pStored)
                               && pExtra.LCapabilityExtraValues.Any(pChoice => string.Equals(pChoice.LCapabilityChoiceValue, pStored, StringComparison.Ordinal))
                ? pStored
                : pExtra.LCapabilityExtraDefault;

            ComboBox pCombo = PSComboBuild(pSelected, PSEncoderChoicesRead(pExtra.LCapabilityExtraValues));
            psAudioExtraCombos[pExtra.LCapabilityExtraOption] = pCombo;
            psAudioRowsPanel.Children.Add(PSFieldBuild(pExtra.LCapabilityExtraLabel, pCombo));
        }
    }
}
