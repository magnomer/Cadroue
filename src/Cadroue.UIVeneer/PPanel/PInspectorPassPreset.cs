using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private static string PFilterKeyRead(PInspectorPass pPass, string pToken)
    {
        foreach (PInspectorPassChoice pEntry in pPass.PInspectorPassPresets)
        {
            if (pEntry.PInspectorPassToken == pToken)
            {
                return pEntry.PInspectorPassKey;
            }
        }

        return string.Empty;
    }

    private static Cadroue.Core.LPassbandPreset? PFilterPresetRead(PInspectorPass pPass) =>
        pPass.PInspectorPassBase is { } pBase
            ? Cadroue.Core.LPassband.LPassbandRead(pPass.PInspectorPassHigh, pBase)
            : null;

    private string? PFilterMatchRead(PInspectorPass pPass) =>
        Cadroue.Core.LPassband.LPassbandMatch(
            pPass.PInspectorPassHigh,
            PInspectorPassRead(pPass),
            PFilterStagesRead(pPass),
            PFilterPolesRead(pPass),
            PFilterResonanceRead(pPass));

    private static void PFilterValuesApply(PInspectorPass pPass, Cadroue.Core.LPassbandPreset pPreset)
    {
        pPass.PInspectorPassSuppress = true;
        pPass.PFilterStageSuppress = true;
        pPass.PInspectorPresetSuppress = true;
        pPass.PInspectorPassFrequency.Value = Math.Clamp(
            pPreset.LPassbandCutoff,
            pPass.PInspectorPassMin,
            pPass.PInspectorPassMax);
        pPass.PInspectorPassValue.Text = pPreset.LPassbandCutoff.ToString("0", CultureInfo.InvariantCulture);
        pPass.PInspectorPassStages.Value = Math.Clamp(
            pPreset.LPassbandStages,
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost);
        pPass.PFilterStageValue.Text = pPreset.LPassbandStages.ToString(CultureInfo.InvariantCulture);
        pPass.PInspectorPassPoles.SelectedIndex = pPreset.LPassbandPoles == 1 ? 0 : 1;
        pPass.PInspectorPassResonance.Text = pPreset.LPassbandResonance.ToString("0.###", CultureInfo.InvariantCulture);
        pPass.PInspectorPassSuppress = false;
        pPass.PFilterStageSuppress = false;
        pPass.PInspectorPresetSuppress = false;
    }

    private void PFilterPresetApply(PInspectorPass pPass)
    {
        if (pPass.PInspectorPresetSuppress)
        {
            return;
        }

        string pName = LLocalizationChoice.LLocalizationChoiceRead(pPass.PInspectorPassPreset.SelectedItem);
        if (string.IsNullOrEmpty(pName) || pName == "Custom"
            || Cadroue.Core.LPassband.LPassbandRead(pPass.PInspectorPassHigh, pName) is not { } pPreset)
        {
            pPass.PInspectorPassBase = null;
            return;
        }

        pPass.PInspectorPassBase = pName;
        PFilterValuesApply(pPass, pPreset);
        PFilterCustomReset(pPass);
        PInspectorActiveRaise();
    }

    private void PFilterDeviationCheck(PInspectorPass pPass)
    {
        if (pPass.PInspectorPresetSuppress || pPass.PInspectorPassBase is not { } pBase
            || Cadroue.Core.LPassband.LPassbandRead(pPass.PInspectorPassHigh, pBase) is null)
        {
            return;
        }

        pPass.PInspectorPresetSuppress = true;
        if (PFilterMatchRead(pPass) == pBase)
        {
            PFilterCustomReset(pPass);
            PFilterPresetSelect(pPass, pBase);
        }
        else
        {
            PFilterCustomSet(pPass, pBase);
        }

        pPass.PInspectorPresetSuppress = false;
        PInspectorActiveRaise();
    }

    private static void PFilterCustomSet(PInspectorPass pPass, string pToken)
    {
        int pLast = pPass.PInspectorPassPreset.Items.Count - 1;
        string pText = LLocalization.LLocalizationFormat(
            "Inspector.Common.PresetCustom",
            LLocalization.LLocalizationTextRead(PFilterKeyRead(pPass, pToken)));
        pPass.PInspectorPassPreset.Items[pLast] = new LLocalizationChoice("Custom", string.Empty, pText);
        pPass.PInspectorPassPreset.SelectedIndex = pLast;
    }

    private static void PFilterCustomReset(PInspectorPass pPass)
    {
        int pLast = pPass.PInspectorPassPreset.Items.Count - 1;
        pPass.PInspectorPassPreset.Items[pLast] = new LLocalizationChoice("Custom", "Inspector.Common.Custom");
    }

    private static void PFilterPresetSelect(PInspectorPass pPass, string pToken)
    {
        for (int pIndex = 0; pIndex < pPass.PInspectorPassPreset.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pPass.PInspectorPassPreset.Items[pIndex]) == pToken)
            {
                pPass.PInspectorPassPreset.SelectedIndex = pIndex;
                return;
            }
        }
    }

    private void PFilterPresetUpdate(PInspectorPass pPass)
    {
        pPass.PInspectorPresetSuppress = true;
        string? pMatch = PFilterMatchRead(pPass);
        if (pMatch is not null)
        {
            pPass.PInspectorPassBase = pMatch;
            PFilterCustomReset(pPass);
            PFilterPresetSelect(pPass, pMatch);
        }
        else
        {
            pPass.PInspectorPassBase = null;
            PFilterCustomReset(pPass);
            pPass.PInspectorPassPreset.SelectedIndex = pPass.PInspectorPassPreset.Items.Count - 1;
        }

        pPass.PInspectorPresetSuppress = false;
    }
}
