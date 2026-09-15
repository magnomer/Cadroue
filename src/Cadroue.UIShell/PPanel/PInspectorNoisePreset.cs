using System.Globalization;
using Cadroue.Core;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private LGrainPreset? PNoisePresetRead() =>
        pNoiseBaseToken is { } pBase ? LGrainCatalog.LGrainRead(pBase) : null;

    private string? PNoiseMatchRead() => LGrainCatalog.LGrainMatch(
        PInspectorDecimalRead(pNoiseReductionValue, 12),
        PInspectorDecimalRead(pNoiseFloor, -50),
        PInspectorDecimalRead(pNoiseSmoothValue, 6),
        PInspectorDecimalRead(pNoiseAdaptivity, 0.5),
        PInspectorDecimalRead(pNoiseResidual, -38),
        PNoiseTypeRead());

    private static string PNoiseKeyRead(string pToken) => pToken switch
    {
        "Light" => "Inspector.Noise.Light",
        "Medium" => "Inspector.Noise.Medium",
        "Strong" => "Inspector.Noise.Strong",
        "Dialogue" => "Inspector.Noise.Dialogue",
        "Vinyl" => "Inspector.Noise.Vinyl",
        "Shellac" => "Inspector.Noise.Shellac",
        _ => "Inspector.Common.Custom"
    };

    private void PNoiseValuesApply(LGrainPreset pPreset)
    {
        pNoiseSuppress = true;
        pNoiseSmoothSuppress = true;
        pNoisePresetSuppress = true;
        pNoiseReduction.Value = Math.Clamp(
            pPreset.LGrainReduction,
            LGrainCatalog.LGrainReductionLeast,
            LGrainCatalog.LGrainReductionMost);
        pNoiseReductionValue.Text = pPreset.LGrainReduction.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseSmooth.Value = Math.Clamp(
            pPreset.LGrainSmooth,
            LGrainCatalog.LGrainSmoothLeast,
            LGrainCatalog.LGrainSmoothMost);
        pNoiseSmoothValue.Text = pPreset.LGrainSmooth.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseFloor.Text = pPreset.LGrainFloor.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseResidual.Text = pPreset.LGrainResidual.ToString("0.#", CultureInfo.InvariantCulture);
        pNoiseAdaptivity.Text = pPreset.LGrainAdaptivity.ToString("0.###", CultureInfo.InvariantCulture);
        pNoiseType.SelectedIndex = pPreset.LGrainType switch
        {
            LGrain.LGrainVinyl => 1,
            LGrain.LGrainShellac => 2,
            _ => 0
        };
        pNoiseSuppress = false;
        pNoiseSmoothSuppress = false;
        pNoisePresetSuppress = false;
    }

    private void PNoisePresetApply()
    {
        if (pNoisePresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pNoisePreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom" || LGrainCatalog.LGrainRead(pName) is not { } pPreset)
        {
            pNoiseBaseToken = null;
            return;
        }

        pNoiseBaseToken = pName;
        PNoiseValuesApply(pPreset);
        PNoiseCustomReset();
        PInspectorActiveRaise();
    }

    private void PNoiseDeviationCheck()
    {
        if (pNoisePresetSuppress || pNoiseBaseToken is not { } pBase
            || LGrainCatalog.LGrainRead(pBase) is null)
        {
            return;
        }

        pNoisePresetSuppress = true;
        if (PNoiseMatchRead() == pBase)
        {
            PNoiseCustomReset();
            PNoisePresetSelect(pBase);
        }
        else
        {
            PNoiseCustomSet(pBase);
        }

        pNoisePresetSuppress = false;
        PInspectorActiveRaise();
    }

    private void PNoiseCustomSet(string pBase)
    {
        int pLast = pNoisePreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PNoiseKeyRead(pBase)));
        pNoisePreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pNoisePreset.SelectedIndex = pLast;
    }

    private void PNoiseCustomReset()
    {
        int pLast = pNoisePreset.Items.Count - 1;
        pNoisePreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private void PNoisePresetSelect(string pToken)
    {
        for (int pIndex = 0; pIndex < pNoisePreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pNoisePreset.Items[pIndex]) == pToken)
            {
                pNoisePreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PNoisePresetUpdate()
    {
        pNoisePresetSuppress = true;
        string? pMatch = PNoiseMatchRead();

        if (pMatch is not null)
        {
            pNoiseBaseToken = pMatch;
            PNoiseCustomReset();
            PNoisePresetSelect(pMatch);
        }
        else
        {
            pNoiseBaseToken = null;
            PNoiseCustomReset();
            pNoisePreset.SelectedIndex = pNoisePreset.Items.Count - 1;
        }

        pNoisePresetSuppress = false;
    }
}
