using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

// One filter section's surface wiring: pulls a fresh sheet, hands every input to its filter adapter, and asks that
// adapter to paint the gate state. The sheet is shared by the high-pass and low-pass instances.
public sealed class QFilterSection
{
    private readonly QFilter qFilter;

    public QFilterSection(QFilter lFilter, string lApplyKey)
    {
        qFilter = lFilter;
        QFilterSectionBody = QContract.QContractSheetFind<StackPanel>("PFilter");
        QFilterSectionPersistent = QContract.QContractSheetFind<CheckBox>("PFilterPersistent");

        CheckBox lActive = QContract.QContractFind<CheckBox>(QFilterSectionBody, "PFilterActive");
        StackPanel lStack = QContract.QContractFind<StackPanel>(QFilterSectionBody, "PFilterStack");
        ComboBox lPreset = QContract.QContractFind<ComboBox>(QFilterSectionBody, "PFilterPreset");
        Slider lFrequency = QContract.QContractFind<Slider>(QFilterSectionBody, "PFilterFrequency");
        TextBox lValue = QContract.QContractFind<TextBox>(QFilterSectionBody, "PFilterValue");
        Slider lStages = QContract.QContractFind<Slider>(QFilterSectionBody, "PFilterStages");
        TextBox lStageValue = QContract.QContractFind<TextBox>(QFilterSectionBody, "PFilterStageValue");
        ComboBox lPoles = QContract.QContractFind<ComboBox>(QFilterSectionBody, "PFilterPoles");
        Slider lResonance = QContract.QContractFind<Slider>(QFilterSectionBody, "PFilterResonance");
        TextBox lResonanceValue =
            QContract.QContractFind<TextBox>(QFilterSectionBody, "PFilterResonanceValue");
        var lHandlers = new List<(Action, Action)>();

        lActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        lActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead(lApplyKey);
        QFilterSectionPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QFilterSectionPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.PersistentTooltip");
        ((TextBlock)((Grid)lPreset.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset");
        ((TextBlock)((Grid)lFrequency.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Cutoff");
        ((TextBlock)((Grid)lStages.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Steepness");
        ((TextBlock)((Grid)lResonance.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Resonance");
        ((TextBlock)((Grid)lPoles.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Pass.Poles");

        lHandlers.Add(QTool.QToolSwitchAttach(lActive, qFilter.QFilterActiveSet));
        lHandlers.Add(QTool.QToolSwitchAttach(QFilterSectionPersistent, qFilter.QFilterPersistentSet));
        QDropdown.QDropdownAttach(lPreset);
        foreach (var lChoice in qFilter.QFilterChoices)
        {
            lPreset.Items.Add(QChoice.QChoiceTextRead(lChoice));
        }

        QDropdown.QDropdownAttach(lPoles);
        foreach (string lName in qFilter.QFilterPoleNames)
        {
            lPoles.Items.Add(lName);
        }

        ComboBox[] lChoices = { lPreset, lPoles };
        Action<int>[] lSelect = { qFilter.QFilterChoiceSelect, qFilter.QFilterPolesSelect };
        for (int lChoice = 0; lChoice < lChoices.Length; lChoice++)
        {
            int lIndex = lChoice;
            SelectionChangedEventHandler lHandler =
                (_, _) => lSelect[lIndex](lChoices[lIndex].SelectedIndex);
            Action lAttach = () =>
            {
                lChoices[lIndex].SelectionChanged -= lHandler;
                lChoices[lIndex].SelectionChanged += lHandler;
            };
            Action lDetach = () => lChoices[lIndex].SelectionChanged -= lHandler;
            lAttach();
            lHandlers.Add((lAttach, lDetach));
        }

        Slider[] lSliders = { lFrequency, lStages, lResonance };
        TextBox[] lValues = { lValue, lStageValue, lResonanceValue };
        for (int lSlot = 0; lSlot < lSliders.Length; lSlot++)
        {
            int lIndex = lSlot;
            Slider lSlider = lSliders[lIndex];
            TextBox lNumber = lValues[lIndex];
            lSlider.Minimum = qFilter.QFilterLeasts[lIndex];
            lSlider.Maximum = qFilter.QFilterMosts[lIndex];
            QSlider.QSliderAttach(lSlider);
            QSlider.QSliderResetAttach(lSlider, () => qFilter.QFilterDefaults[lIndex]);
            QTextbox.QTextboxAttach(lNumber);
            lNumber.PreviewTextInput +=
                (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
            RoutedPropertyChangedEventHandler<double> lSlide =
                (_, _) => qFilter.QFilterValueSet(lIndex, lSlider.Value);
            Action lCommit = () =>
            {
                lSlider.Value = QInspectorField.QInspectorValueCommit(
                    lNumber.Text,
                    qFilter.QFilterValues[lIndex],
                    qFilter.QFilterLeasts[lIndex],
                    qFilter.QFilterMosts[lIndex]);
                lNumber.CaretIndex = lNumber.Text.Length;
            };
            RoutedEventHandler lLost = (_, _) => lCommit();
            KeyEventHandler lKey = (_, lInput) =>
            {
                if (lInput.Key == Key.Enter)
                {
                    lCommit();
                }

                lInput.Handled = lInput.Key == Key.Enter;
            };
            Action lAttach = () =>
            {
                lSlider.ValueChanged -= lSlide;
                lNumber.LostFocus -= lLost;
                lNumber.KeyDown -= lKey;
                lSlider.ValueChanged += lSlide;
                lNumber.LostFocus += lLost;
                lNumber.KeyDown += lKey;
            };
            Action lDetach = () =>
            {
                lSlider.ValueChanged -= lSlide;
                lNumber.LostFocus -= lLost;
                lNumber.KeyDown -= lKey;
            };
            lAttach();
            lHandlers.Add((lAttach, lDetach));
        }
        lStages.IsSnapToTickEnabled = true;
        QFilterSectionBody.Resources[typeof(QFilterSection)] = lHandlers;
        QFilterSectionAttach();
    }

    public StackPanel QFilterSectionBody { get; }

    public CheckBox QFilterSectionPersistent { get; }

    public void QFilterSectionAttach()
    {
        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)QFilterSectionBody.Resources[typeof(QFilterSection)])
        {
            lAttach();
        }

        qFilter.QFilterChanged -= QFilterSectionUpdate;
        qFilter.QFilterChanged += QFilterSectionUpdate;
    }

    public void QFilterSectionDetach()
    {
        qFilter.QFilterChanged -= QFilterSectionUpdate;
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)QFilterSectionBody.Resources[typeof(QFilterSection)])
        {
            lDetach();
        }
    }

    public void QFilterSectionUpdate()
    {
        Slider lResonance = QContract.QContractFind<Slider>(QFilterSectionBody, "PFilterResonance");
        qFilter.QFilterUpdate(
            QContract.QContractFind<CheckBox>(QFilterSectionBody, "PFilterActive"),
            QFilterSectionPersistent,
            [
                (
                    QContract.QContractFind<Slider>(QFilterSectionBody, "PFilterFrequency"),
                    QContract.QContractFind<TextBox>(QFilterSectionBody, "PFilterValue")),
                (
                    QContract.QContractFind<Slider>(QFilterSectionBody, "PFilterStages"),
                    QContract.QContractFind<TextBox>(QFilterSectionBody, "PFilterStageValue")),
                (lResonance, QContract.QContractFind<TextBox>(QFilterSectionBody, "PFilterResonanceValue"))
            ],
            QContract.QContractFind<ComboBox>(QFilterSectionBody, "PFilterPoles"),
            (Grid)lResonance.Parent,
            QContract.QContractFind<ComboBox>(QFilterSectionBody, "PFilterPreset"),
            QContract.QContractFind<StackPanel>(QFilterSectionBody, "PFilterStack"));
    }
}
