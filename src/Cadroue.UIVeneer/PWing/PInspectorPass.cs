using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private sealed record PInspectorPass(
        LFilter PFilterOwner,
        CheckBox PFilterApplyBox,
        CheckBox PInspectorPassPersistent,
        ComboBox PInspectorPassPreset,
        Slider PInspectorPassFrequency,
        TextBox PInspectorPassValue,
        Slider PInspectorPassStages,
        TextBox PFilterStageValue,
        ComboBox PInspectorPassPoles,
        Slider PFilterResonanceSlider,
        TextBox PInspectorPassResonance,
        Grid PFilterResonanceRow,
        StackPanel PInspectorPassStack,
        StackPanel PInspectorPassBody);

    private PInspectorPass pInspectorHighPass = null!;
    private PInspectorPass pInspectorLowPass = null!;

    private StackPanel PFilterHighBuild()
    {
        pInspectorHighPass = PInspectorPassBuild(
            LFilterHigh, LLocalization.LLocalizationTextRead("Inspector.Pass.HighApply"));
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PFilterLowBuild()
    {
        pInspectorLowPass = PInspectorPassBuild(
            LFilterLow, LLocalization.LLocalizationTextRead("Inspector.Pass.LowApply"));
        return pInspectorLowPass.PInspectorPassBody;
    }

    private static PInspectorPass PInspectorPassBuild(LFilter lOwner, string pApplyTip)
    {
        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), pApplyTip);
        CheckBox pPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Pass.PersistentTooltip"));
        PInspectorSwitchAttach(pApply, lOwner.LFilterActiveSet);
        PInspectorSwitchAttach(pPersistent, lOwner.LFilterPersistentSet);

        ComboBox pPreset = PInspectorChoiceBuild(
            lOwner.LFilterChoiceRead().LInspectorChoiceNames, lOwner.LFilterChoiceSelect);

        Slider pFrequency = PInspectorSliderBuild(
            lOwner.LFilterFloor,
            lOwner.LFilterCeiling,
            lOwner.LFilterStep.LWorkPassFrequency,
            () => lOwner.LFilterDefaultRead(0));
        TextBox pValue = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pFrequency,
            pValue,
            lOwner.LFilterFloor,
            lOwner.LFilterCeiling,
            () => lOwner.LFilterStep.LWorkPassFrequency,
            lOwner.LFilterFrequencySet);

        Slider pStages = PInspectorSliderBuild(
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            1,
            () => lOwner.LFilterDefaultRead(1));
        pStages.IsSnapToTickEnabled = true;
        pStages.TickFrequency = 1;
        TextBox pStageValue = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pStages,
            pStageValue,
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            () => lOwner.LFilterStep.LWorkPassStages,
            pNumber => lOwner.LFilterStagesSet((int)Math.Round(pNumber)));

        ComboBox pPoles = PInspectorChoiceBuild(lOwner.LFilterPoles, lOwner.LFilterPolesSelect);
        pPoles.Width = 120;

        Slider pResonanceSlider = PInspectorSliderBuild(
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            lOwner.LFilterStep.LWorkPassResonance,
            () => lOwner.LFilterDefaultRead(2));
        TextBox pResonance = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pResonanceSlider,
            pResonance,
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            () => lOwner.LFilterStep.LWorkPassResonance,
            lOwner.LFilterResonanceSet);

        var pStack = new StackPanel();
        var pBody = new StackPanel { Margin = new Thickness(12, 12, 12, 12), Visibility = Visibility.Collapsed };
        pStack.Children.Add(
            PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Common.Preset"), pPreset));
        pStack.Children.Add(
            PInspectorSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Pass.Cutoff"), pFrequency, "Hz", pValue));
        pStack.Children.Add(
            PInspectorSliderBuild(
                LLocalization.LLocalizationTextRead("Inspector.Pass.Steepness"),
                pStages,
                "×12dB",
                pStageValue));
        Grid pResonanceRow = PInspectorSliderBuild(
            LLocalization.LLocalizationTextRead("Inspector.Pass.Resonance"),
            pResonanceSlider,
            "Q",
            pResonance);
        pStack.Children.Add(pResonanceRow);
        pStack.Children.Add(PInspectorFieldBuild(LLocalization.LLocalizationTextRead("Inspector.Pass.Poles"), pPoles));

        pBody.Children.Add(pApply);
        pBody.Children.Add(PInspectorSeparatorBuild());
        pBody.Children.Add(pStack);

        return new PInspectorPass(
            lOwner,
            pApply,
            pPersistent,
            pPreset,
            pFrequency,
            pValue,
            pStages,
            pStageValue,
            pPoles,
            pResonanceSlider,
            pResonance,
            pResonanceRow,
            pStack,
            pBody);
    }

    private static void PFilterUpdate(PInspectorPass pPass)
    {
        LFilter lOwner = pPass.PFilterOwner;
        bool lActive = lOwner.LFilterStep.LWorkStepActive;
        PInspectorSwitchUpdate(pPass.PFilterApplyBox, lActive);
        PInspectorSwitchUpdate(pPass.PInspectorPassPersistent, lOwner.LFilterPersistent);
        PInspectorValueUpdate(
            pPass.PInspectorPassFrequency, pPass.PInspectorPassValue, lOwner.LFilterStep.LWorkPassFrequency, "0");
        PInspectorValueUpdate(
            pPass.PInspectorPassStages, pPass.PFilterStageValue, lOwner.LFilterStep.LWorkPassStages, "0");
        PInspectorValueUpdate(
            pPass.PFilterResonanceSlider,
            pPass.PInspectorPassResonance,
            lOwner.LFilterStep.LWorkPassResonance,
            "0.###");
        pPass.PInspectorPassPoles.SelectedIndex = lOwner.LFilterPolesIndex;
        PInspectorSectionUpdate(pPass.PFilterResonanceRow, lOwner.LFilterResonanceActive);
        PInspectorChoiceApply(pPass.PInspectorPassPreset, lOwner.LFilterChoiceRead());
        PInspectorSectionUpdate(pPass.PInspectorPassStack, lActive);
    }
}
