using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private bool pEqualizerPresetSuppress;
    private string? pEqualizerBaseToken;

    private static string PEqualizerKeyRead(string pToken) => pToken switch
    {
        "Flat" => "Inspector.Equalizer.Preset.Flat",
        "Bass boost" => "Inspector.Equalizer.Preset.BassBoost",
        "Bright" => "Inspector.Equalizer.Preset.Bright",
        "Warm" => "Inspector.Equalizer.Preset.Warm",
        "Loudness" => "Inspector.Equalizer.Preset.Loudness",
        "Vocal" => "Inspector.Equalizer.Preset.Vocal",
        "De-ess" => "Inspector.Equalizer.Preset.Deess",
        "Podcast" => "Inspector.Equalizer.Preset.Podcast",
        "Telephone" => "Inspector.Equalizer.Preset.Telephone",
        _ => "Inspector.Common.Custom"
    };

    private void PEqualizerPresetApply()
    {
        if (pEqualizerPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pEqualizerPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom"
            || LContourCatalog.LContourGainsRead(pName) is not { } pGains)
        {
            pEqualizerBaseToken = null;
            return;
        }

        pEqualizerPresetSuppress = true;
        pEqualizerBaseToken = pName;
        PEqualizerRowsApply(pGains);
        PEqualizerCustomReset();
        pEqualizerPresetSuppress = false;
        PInspectorActiveRaise();
    }

    private void PEqualizerDeviationCheck()
    {
        if (pEqualizerPresetSuppress || pEqualizerBaseToken is not { } pBase
            || LContourCatalog.LContourGainsRead(pBase) is not { } pGains)
        {
            return;
        }

        (double[] pFrequencies, double[] pCurrentGains) = PEqualizerCurrentRead();
        pEqualizerPresetSuppress = true;
        if (LContourCatalog.LContourMatch(pFrequencies, pCurrentGains, pGains))
        {
            PEqualizerCustomReset();
            PEqualizerPresetSelect(pBase);
        }
        else
        {
            PEqualizerCustomSet(pBase);
        }

        pEqualizerPresetSuppress = false;
    }

    private void PEqualizerPresetUpdate()
    {
        pEqualizerPresetSuppress = true;
        (double[] pFrequencies, double[] pGains) = PEqualizerCurrentRead();
        string? pMatch = LContourCatalog.LContourPresetFind(pFrequencies, pGains);
        if (pMatch is not null)
        {
            pEqualizerBaseToken = pMatch;
            PEqualizerCustomReset();
            PEqualizerPresetSelect(pMatch);
        }
        else
        {
            pEqualizerBaseToken = null;
            PEqualizerCustomReset();
            pEqualizerPreset.SelectedIndex = pEqualizerPreset.Items.Count - 1;
        }

        pEqualizerPresetSuppress = false;
    }

    private void PEqualizerCustomSet(string pBase)
    {
        int pLast = pEqualizerPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PEqualizerKeyRead(pBase)));
        pEqualizerPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pEqualizerPreset.SelectedIndex = pLast;
    }

    private void PEqualizerCustomReset()
    {
        int pLast = pEqualizerPreset.Items.Count - 1;
        pEqualizerPreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PEqualizerPresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pEqualizerPreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pEqualizerPreset.Items[pIndex]) == pToken)
            {
                pEqualizerPreset.SelectedIndex = pIndex;
                return;
            }
        }
    }
}
