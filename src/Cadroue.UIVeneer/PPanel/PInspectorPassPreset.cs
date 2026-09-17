using System.Windows.Controls;
using Cadroue.Core;

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

        return "Inspector.Common.Custom";
    }

    private void PFilterUpdate(PInspectorPass pPass)
    {
        LWorkPassStep pStep = pPass.PFilterOwner.LFilterStep;
        PInspectorSwitchUpdate(pPass.PFilterApplyBox, pStep.LWorkStepActive, false);
        PInspectorSwitchUpdate(pPass.PInspectorPassPersistent, pPass.PFilterOwner.LFilterPersistent, true);
        PInspectorValueUpdate(pPass.PInspectorPassFrequency, pPass.PInspectorPassValue, pStep.LWorkPassFrequency, "0");
        PInspectorValueUpdate(pPass.PInspectorPassStages, pPass.PFilterStageValue, pStep.LWorkPassStages, "0");
        PInspectorValueUpdate(
            pPass.PFilterResonanceSlider, pPass.PInspectorPassResonance, pStep.LWorkPassResonance, "0.###");
        int pPoles = pStep.LWorkPassPoles == 1 ? 0 : 1;
        if (pPass.PInspectorPassPoles.SelectedIndex != pPoles)
        {
            pPass.PInspectorPassPoles.SelectedIndex = pPoles;
        }

        PInspectorSectionUpdate(pPass.PFilterResonanceRow, pStep.LWorkPassPoles == 2);
        PInspectorPresetUpdate(
            pPass.PInspectorPassPreset,
            pPass.PFilterOwner.LFilterMatchRead(),
            pPass.PFilterOwner.LFilterToken,
            pToken => PFilterKeyRead(pPass, pToken));
        PInspectorSectionUpdate(pPass.PInspectorPassStack, pStep.LWorkStepActive);
    }
}
