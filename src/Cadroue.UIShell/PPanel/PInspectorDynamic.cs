using System.Globalization;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private (double Frame, double Gauss, double MaxGain, double Compress)? PDynamicPresetRead() =>
        pLoudnessBaseToken is { } pBase ? LLevelingCatalog.LLevelingDynamicRead(pBase) : null;

    private static string PDynamicKeyRead(string pToken) => pToken switch
    {
        "Gentle" => "Inspector.Dynamic.Gentle",
        "Leveler" => "Inspector.Dynamic.Leveler",
        "Voice" => "Inspector.Dynamic.Voice",
        "Aggressive" => "Inspector.Dynamic.Aggressive",
        "Music" => "Inspector.Dynamic.Music",
        _ => "Inspector.Common.Custom"
    };

    private string? PDynamicValuesMatch() =>
        LLevelingCatalog.LLevelingDynamicMatch(
            PInspectorDecimalRead(pDynamicFrame, 300),
            PInspectorDecimalRead(pDynamicGauss, 21),
            PInspectorDecimalRead(pDynamicMaxGain, 10),
            PInspectorDecimalRead(pDynamicCompress, 6));

    private void PDynamicValuesApply((double Frame, double Gauss, double MaxGain, double Compress) pPreset)
    {
        pLoudnessPresetSuppress = true;
        pDynamicFrame.Text = pPreset.Frame.ToString("0.###", CultureInfo.InvariantCulture);
        pDynamicGauss.Text = pPreset.Gauss.ToString("0.###", CultureInfo.InvariantCulture);
        pDynamicMaxGain.Text = pPreset.MaxGain.ToString("0.###", CultureInfo.InvariantCulture);
        pDynamicCompress.Text = pPreset.Compress.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessPresetSuppress = false;
    }

    private void PDynamicPresetApply()
    {
        if (pLoudnessPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pLoudnessPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || LLevelingCatalog.LLevelingDynamicRead(pName) is not { } pPreset)
        {
            pLoudnessBaseToken = null;
            return;
        }

        pLoudnessBaseToken = pName;
        PDynamicValuesApply(pPreset);
        PLoudnessCustomReset();
        PInspectorActiveRaise();
    }

    private void PDynamicDeviationCheck()
    {
        if (pLoudnessPresetSuppress || pLoudnessBaseToken is not { } pBase
            || LLevelingCatalog.LLevelingDynamicRead(pBase) is null)
        {
            return;
        }

        pLoudnessPresetSuppress = true;
        if (PDynamicValuesMatch() == pBase)
        {
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pBase);
        }
        else
        {
            PDynamicCustomSet(pBase);
        }

        pLoudnessPresetSuppress = false;
    }

    private void PDynamicValueUpdate()
    {
        PDynamicDeviationCheck();
        PInspectorActiveRaise();
    }

    private void PDynamicCustomSet(string pBase)
    {
        int pLast = pLoudnessPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PDynamicKeyRead(pBase)));
        pLoudnessPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pLoudnessPreset.SelectedIndex = pLast;
    }
}
