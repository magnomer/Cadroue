using System.Globalization;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private LLevelingLoudnessPreset? PLoudnessPresetRead() =>
        pLoudnessBaseToken is { } pBase ? LLevelingCatalog.LLevelingLoudnessRead(pBase) : null;

    private static string PLoudnessKeyRead(string pToken) => pToken switch
    {
        "Loud" => "Inspector.Normalize.Loud",
        "Streaming" => "Inspector.Normalize.Streaming",
        "Podcast" => "Inspector.Normalize.Podcast",
        "Dialogue" => "Inspector.Normalize.Dialogue",
        "Audiobook" => "Inspector.Normalize.Audiobook",
        "Broadcast" => "Inspector.Normalize.Broadcast",
        "TV" => "Inspector.Normalize.TV",
        "Film" => "Inspector.Normalize.Film",
        _ => "Inspector.Common.Custom"
    };

    private string? PLoudnessValuesMatch() =>
        LLevelingCatalog.LLevelingLoudnessMatch(
            PInspectorDecimalRead(pLoudnessTarget, -16),
            PInspectorDecimalRead(pLoudnessPeak, -1.5),
            PInspectorDecimalRead(pLoudnessRange, 11));

    private void PLoudnessValuesApply(LLevelingLoudnessPreset pPreset)
    {
        pLoudnessPresetSuppress = true;
        pLoudnessTarget.Text = pPreset.LLevelingTarget.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessPeak.Text = pPreset.LLevelingPeak.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessRange.Text = pPreset.LLevelingRange.ToString("0.###", CultureInfo.InvariantCulture);
        pLoudnessPresetSuppress = false;
    }

    private void PLoudnessPresetApply()
    {
        if (pLoudnessPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pLoudnessPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName)
            || pName == "Custom"
            || LLevelingCatalog.LLevelingLoudnessRead(pName) is not { } pPreset)
        {
            pLoudnessBaseToken = null;
            return;
        }

        pLoudnessBaseToken = pName;
        PLoudnessValuesApply(pPreset);
        PLoudnessCustomReset();
        PInspectorActiveRaise();
    }

    private void PLoudnessDeviationCheck()
    {
        if (pLoudnessPresetSuppress || pLoudnessBaseToken is not { } pBase
            || LLevelingCatalog.LLevelingLoudnessRead(pBase) is null)
        {
            return;
        }

        pLoudnessPresetSuppress = true;
        if (PLoudnessValuesMatch() == pBase)
        {
            PLoudnessCustomReset();
            PLoudnessPresetSelect(pBase);
        }
        else
        {
            PLoudnessCustomSet(pBase);
        }

        pLoudnessPresetSuppress = false;
    }

    private void PLoudnessValueUpdate()
    {
        PLoudnessDeviationCheck();
        PInspectorActiveRaise();
    }

    private void PLoudnessCustomSet(string pBase)
    {
        int pLast = pLoudnessPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PLoudnessKeyRead(pBase)));
        pLoudnessPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pLoudnessPreset.SelectedIndex = pLast;
    }

}
