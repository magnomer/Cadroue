using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
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
            LFilterHigh, QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.HighApply"));
        return pInspectorHighPass.PInspectorPassBody;
    }

    private StackPanel PFilterLowBuild()
    {
        pInspectorLowPass = PInspectorPassBuild(
            LFilterLow, QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.LowApply"));
        return pInspectorLowPass.PInspectorPassBody;
    }

    private static PInspectorPass PInspectorPassBuild(QFilter lOwner, string pApplyTip)
    {
        PInspectorPass pPass = PFilterPanelBuild(lOwner, pApplyTip);
        PFilterAttach(pPass);
        return pPass;
    }

    private static PInspectorPass PFilterPanelBuild(QFilter lOwner, string pApplyTip)
    {
        CheckBox pApply = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"), pApplyTip);
        CheckBox pPersistent = PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.PersistentTooltip"));

        ComboBox pPreset = PInspectorChoiceBuild(
            lOwner.QFilterChoiceRead().QInspectorChoiceNames, lOwner.QFilterChoiceSelect);

        Slider pFrequency = PInspectorSliderBuild(
            lOwner.QFilterFloor,
            lOwner.QFilterCeiling,
            lOwner.QFilterStep.LWorkPassFrequency,
            () => lOwner.QFilterDefaultRead(0));
        TextBox pValue = PInspectorDecimalBuild();

        Slider pStages = PInspectorSliderBuild(
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            1,
            () => lOwner.QFilterDefaultRead(1));
        pStages.IsSnapToTickEnabled = true;
        pStages.TickFrequency = 1;
        TextBox pStageValue = PInspectorDecimalBuild();

        ComboBox pPoles = PInspectorChoiceBuild(lOwner.QFilterPoles, lOwner.QFilterPolesSelect);
        pPoles.Width = 120;

        Slider pResonanceSlider = PInspectorSliderBuild(
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            lOwner.QFilterStep.LWorkPassResonance,
            () => lOwner.QFilterDefaultRead(2));
        TextBox pResonance = PInspectorDecimalBuild();

        var pStack = new StackPanel();
        var pBody = new StackPanel { Margin = new Thickness(12, 12, 12, 12), Visibility = Visibility.Collapsed };
        pStack.Children.Add(
            PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset"), pPreset));
        pStack.Children.Add(
            PInspectorSliderBuild(
                QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Cutoff"), pFrequency, "Hz", pValue));
        pStack.Children.Add(
            PInspectorSliderBuild(
                QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Steepness"),
                pStages,
                "×12dB",
                pStageValue));
        Grid pResonanceRow = PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Resonance"),
            pResonanceSlider,
            "Q",
            pResonance);
        pStack.Children.Add(pResonanceRow);
        pStack.Children.Add(PInspectorFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Poles"),
            pPoles));

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

    private static void PFilterAttach(PInspectorPass pPass)
    {
        QFilter lOwner = pPass.PFilterOwner;
        PInspectorSwitchAttach(pPass.PFilterApplyBox, lOwner.QFilterActiveSet);
        PInspectorSwitchAttach(pPass.PInspectorPassPersistent, lOwner.QFilterPersistentSet);
        PInspectorValueAttach(
            pPass.PInspectorPassFrequency,
            pPass.PInspectorPassValue,
            lOwner.QFilterFloor,
            lOwner.QFilterCeiling,
            () => lOwner.QFilterStep.LWorkPassFrequency,
            lOwner.QFilterFrequencySet);
        PInspectorValueAttach(
            pPass.PInspectorPassStages,
            pPass.PFilterStageValue,
            LPassband.LPassbandStagesLeast,
            LPassband.LPassbandStagesMost,
            () => lOwner.QFilterStep.LWorkPassStages,
            pNumber => lOwner.QFilterStagesSet((int)Math.Round(pNumber)));
        PInspectorValueAttach(
            pPass.PFilterResonanceSlider,
            pPass.PInspectorPassResonance,
            LPassband.LPassbandResonanceLeast,
            LPassband.LPassbandResonanceMost,
            () => lOwner.QFilterStep.LWorkPassResonance,
            lOwner.QFilterResonanceSet);
    }

    private static void PFilterUpdate(PInspectorPass pPass)
    {
        QFilter lOwner = pPass.PFilterOwner;
        bool lActive = lOwner.QFilterStep.LWorkStepActive;
        PFilterStateApply(pPass, lActive);
        PInspectorSectionUpdate(pPass.PFilterResonanceRow, lOwner.QFilterResonanceActive);
        PInspectorChoiceApply(pPass.PInspectorPassPreset, lOwner.QFilterChoiceRead());
        PInspectorSectionUpdate(pPass.PInspectorPassStack, lActive);
    }

    private static void PFilterStateApply(PInspectorPass pPass, bool lActive)
    {
        QFilter lOwner = pPass.PFilterOwner;
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
    }
}
