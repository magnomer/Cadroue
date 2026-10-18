using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PInspectorPass
{
    private readonly QFilter pFilterOwner;
    private readonly CheckBox pFilterApplyBox;
    private readonly ComboBox pInspectorPassPreset;
    private readonly Slider pInspectorPassFrequency;
    private readonly TextBox pInspectorPassValue;
    private readonly Slider pInspectorPassStages;
    private readonly TextBox pFilterStageValue;
    private readonly ComboBox pInspectorPassPoles;
    private readonly Slider pFilterResonanceSlider;
    private readonly TextBox pInspectorPassResonance;
    private readonly Grid pFilterResonanceRow;
    private readonly StackPanel pInspectorPassStack;

    internal PInspectorPass(QFilter lOwner, string pApplyTip)
    {
        pFilterOwner = lOwner;
        pFilterApplyBox = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply"), pApplyTip);
        PInspectorPassPersistent = PInspectorSwitch.PInspectorSwitchBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent"),
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.PersistentTooltip"));

        pInspectorPassPreset = PInspectorChoice.PInspectorChoiceBuild(
            lOwner.QFilterChoiceNames, lOwner.QFilterChoiceSelect);

        pInspectorPassFrequency = PInspectorValue.PInspectorSliderBuild(
            lOwner.QFilterLeasts[0],
            lOwner.QFilterMosts[0],
            lOwner.QFilterValueRead(0),
            () => lOwner.QFilterDefaultRead(0));
        pInspectorPassValue = PInspectorNumber.PInspectorDecimalBuild();

        pInspectorPassStages = PInspectorValue.PInspectorSliderBuild(
            lOwner.QFilterLeasts[1],
            lOwner.QFilterMosts[1],
            1,
            () => lOwner.QFilterDefaultRead(1));
        pInspectorPassStages.IsSnapToTickEnabled = true;
        pInspectorPassStages.TickFrequency = 1;
        pFilterStageValue = PInspectorNumber.PInspectorDecimalBuild();

        pInspectorPassPoles = PInspectorChoice.PInspectorChoiceBuild(
            lOwner.QFilterPoleNames, lOwner.QFilterPolesSelect);
        pInspectorPassPoles.Width = 120;

        pFilterResonanceSlider = PInspectorValue.PInspectorSliderBuild(
            lOwner.QFilterLeasts[2],
            lOwner.QFilterMosts[2],
            lOwner.QFilterValueRead(2),
            () => lOwner.QFilterDefaultRead(2));
        pInspectorPassResonance = PInspectorNumber.PInspectorDecimalBuild();

        pInspectorPassStack = new StackPanel();
        PInspectorPassBody = new StackPanel
        {
            Margin = new Thickness(12, 12, 12, 12), Visibility = Visibility.Collapsed
        };
        pInspectorPassStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset"), pInspectorPassPreset));
        pInspectorPassStack.Children.Add(
            PInspectorField.PInspectorSliderBuild(
                QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Cutoff"),
                pInspectorPassFrequency,
                "Hz",
                pInspectorPassValue));
        pInspectorPassStack.Children.Add(
            PInspectorField.PInspectorSliderBuild(
                QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Steepness"),
                pInspectorPassStages,
                "×12dB",
                pFilterStageValue));
        pFilterResonanceRow = PInspectorField.PInspectorSliderBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Resonance"),
            pFilterResonanceSlider,
            "Q",
            pInspectorPassResonance);
        pInspectorPassStack.Children.Add(pFilterResonanceRow);
        pInspectorPassStack.Children.Add(PInspectorField.PInspectorFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Poles"), pInspectorPassPoles));

        PInspectorPassBody.Children.Add(pFilterApplyBox);
        PInspectorPassBody.Children.Add(PInspectorField.PInspectorSeparatorBuild());
        PInspectorPassBody.Children.Add(pInspectorPassStack);
        PFilterAttach();
    }

    internal CheckBox PInspectorPassPersistent { get; }

    internal StackPanel PInspectorPassBody { get; }

    private void PFilterAttach()
    {
        QFilter lOwner = pFilterOwner;
        PInspectorSwitch.PInspectorSwitchAttach(pFilterApplyBox, lOwner.QFilterActiveSet);
        PInspectorSwitch.PInspectorSwitchAttach(PInspectorPassPersistent, lOwner.QFilterPersistentSet);
        PInspectorValue.PInspectorValueAttach(
            pInspectorPassFrequency,
            pInspectorPassValue,
            lOwner.QFilterLeasts[0],
            lOwner.QFilterMosts[0],
            () => lOwner.QFilterValueRead(0),
            pNumber => lOwner.QFilterValueSet(0, pNumber));
        PInspectorValue.PInspectorValueAttach(
            pInspectorPassStages,
            pFilterStageValue,
            lOwner.QFilterLeasts[1],
            lOwner.QFilterMosts[1],
            () => lOwner.QFilterValueRead(1),
            pNumber => lOwner.QFilterValueSet(1, pNumber));
        PInspectorValue.PInspectorValueAttach(
            pFilterResonanceSlider,
            pInspectorPassResonance,
            lOwner.QFilterLeasts[2],
            lOwner.QFilterMosts[2],
            () => lOwner.QFilterValueRead(2),
            pNumber => lOwner.QFilterValueSet(2, pNumber));
    }

    internal void PFilterChangeAttach() => pFilterOwner.QFilterChanged += PFilterUpdate;

    // The driver paints the section; rows come in the gate's slot order.
    internal void PFilterUpdate() =>
        pFilterOwner.QFilterUpdate(
            pFilterApplyBox,
            PInspectorPassPersistent,
            [
                (pInspectorPassFrequency, pInspectorPassValue),
                (pInspectorPassStages, pFilterStageValue),
                (pFilterResonanceSlider, pInspectorPassResonance)
            ],
            pInspectorPassPoles,
            pFilterResonanceRow,
            pInspectorPassPreset,
            pInspectorPassStack);
}
