using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private double PInspectorPassRead(PInspectorPass pPass) =>
        Math.Clamp(PInspectorDecimalRead(pPass.PInspectorPassValue, pPass.PInspectorPassDefault),
            pPass.PInspectorPassMin, pPass.PInspectorPassMax);

    private int PFilterStagesRead(PInspectorPass pPass) =>
        (int)Math.Round(PInspectorDecimalRead(pPass.PFilterStageValue, 1));

    private static int PFilterPolesRead(PInspectorPass pPass) =>
        pPass.PInspectorPassPoles.SelectedIndex == 0 ? 1 : 2;

    private double PFilterResonanceRead(PInspectorPass pPass) =>
        PInspectorDecimalRead(pPass.PInspectorPassResonance, 0.707);

    private void PFilterActiveSet(PInspectorPass pPass, Cadroue.Core.LWorkPassStep pStep)
    {
        pPass.PFilterApplyBox.IsChecked = pStep.LWorkStepActive;
        pPass.PInspectorPassSuppress = true;
        pPass.PFilterStageSuppress = true;
        pPass.PInspectorPresetSuppress = true;
        pPass.PInspectorPassFrequency.Value = Math.Clamp(
            pStep.LWorkPassFrequency,
            pPass.PInspectorPassMin,
            pPass.PInspectorPassMax);
        pPass.PInspectorPassValue.Text = pStep.LWorkPassFrequency.ToString("0", CultureInfo.InvariantCulture);
        pPass.PInspectorPassStages.Value = Math.Clamp(
            pStep.LWorkPassStages,
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost);
        pPass.PFilterStageValue.Text = pStep.LWorkPassStages.ToString(CultureInfo.InvariantCulture);
        pPass.PInspectorPassPoles.SelectedIndex = pStep.LWorkPassPoles == 1 ? 0 : 1;
        pPass.PInspectorPassResonance.Text = pStep.LWorkPassResonance.ToString("0.###", CultureInfo.InvariantCulture);
        pPass.PInspectorPassSuppress = false;
        pPass.PFilterStageSuppress = false;
        pPass.PInspectorPresetSuppress = false;
        PFilterPresetUpdate(pPass);
        PFilterApplyUpdate(pPass);
    }

    private void PFilterApplyUpdate(PInspectorPass pPass)
    {
        bool pActive = pPass.PFilterApplyBox.IsChecked == true;
        pPass.PInspectorPassStack.IsEnabled = pActive;
        pPass.PInspectorPassStack.Opacity = pActive ? 1 : 0.4;
        PInspectorActiveRaise();
    }
}
