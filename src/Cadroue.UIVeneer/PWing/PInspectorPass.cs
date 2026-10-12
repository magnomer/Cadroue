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
        QFilter PFilterOwner,
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

    private static PInspectorPass PInspectorPassBuild(QFilter lOwner, string pApplyTip)
    {
        CheckBox pApply = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Apply"), pApplyTip);
        CheckBox pPersistent = PInspectorSwitchBuild(
            LLocalization.LLocalizationTextRead("Inspector.Common.Persistent"),
            LLocalization.LLocalizationTextRead("Inspector.Pass.PersistentTooltip"));
        PInspectorSwitchAttach(pApply, lOwner.QFilterActiveSet);
        PInspectorSwitchAttach(pPersistent, lOwner.QFilterPersistentSet);

        ComboBox pPreset = PInspectorChoiceBuild(
            lOwner.QFilterChoiceRead().QInspectorChoiceNames, lOwner.QFilterChoiceSelect);

        Slider pFrequency = PInspectorSliderBuild(
            lOwner.QFilterFloor,
            lOwner.QFilterCeiling,
            lOwner.QFilterStep.LWorkPassFrequency,
            () => lOwner.QFilterDefaultRead(0));
        TextBox pValue = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pFrequency,
            pValue,
            lOwner.QFilterFloor,
            lOwner.QFilterCeiling,
            () => lOwner.QFilterStep.LWorkPassFrequency,
            lOwner.QFilterFrequencySet);

        Slider pStages = PInspectorSliderBuild(
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            1,
            () => lOwner.QFilterDefaultRead(1));
        pStages.IsSnapToTickEnabled = true;
        pStages.TickFrequency = 1;
        TextBox pStageValue = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pStages,
            pStageValue,
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            () => lOwner.QFilterStep.LWorkPassStages,
            pNumber => lOwner.QFilterStagesSet((int)Math.Round(pNumber)));

        ComboBox pPoles = PInspectorChoiceBuild(lOwner.QFilterPoles, lOwner.QFilterPolesSelect);
        pPoles.Width = 120;

        Slider pResonanceSlider = PInspectorSliderBuild(
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            lOwner.QFilterStep.LWorkPassResonance,
            () => lOwner.QFilterDefaultRead(2));
        TextBox pResonance = PInspectorDecimalBuild();
        PInspectorValueAttach(
            pResonanceSlider,
            pResonance,
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            () => lOwner.QFilterStep.LWorkPassResonance,
            lOwner.QFilterResonanceSet);

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
        QFilter lOwner = pPass.PFilterOwner;
        bool lActive = lOwner.QFilterStep.LWorkStepActive;
        PInspectorSwitchUpdate(pPass.PFilterApplyBox, lActive);
        PInspectorSwitchUpdate(pPass.PInspectorPassPersistent, lOwner.QFilterPersistent);
        PInspectorValueUpdate(
            pPass.PInspectorPassFrequency, pPass.PInspectorPassValue, lOwner.QFilterStep.LWorkPassFrequency, "0");
        PInspectorValueUpdate(
            pPass.PInspectorPassStages, pPass.PFilterStageValue, lOwner.QFilterStep.LWorkPassStages, "0");
        PInspectorValueUpdate(
            pPass.PFilterResonanceSlider,
            pPass.PInspectorPassResonance,
            lOwner.QFilterStep.LWorkPassResonance,
            "0.###");
        pPass.PInspectorPassPoles.SelectedIndex = lOwner.QFilterPolesIndex;
        PInspectorSectionUpdate(pPass.PFilterResonanceRow, lOwner.QFilterResonanceActive);
        PInspectorChoiceApply(pPass.PInspectorPassPreset, lOwner.QFilterChoiceRead());
        PInspectorSectionUpdate(pPass.PInspectorPassStack, lActive);
    }
}
