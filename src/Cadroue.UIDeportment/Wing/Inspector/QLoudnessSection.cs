using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

// The loudness section's surface wiring: pulls the combined two-mode sheet, maps its two modes and seven
// fixed rows to the driver's ordered slots, hands every input to the driver, and asks the driver to paint gate state.
public sealed class QLoudnessSection
{
    private readonly QLoudness qLoudness;

    public QLoudnessSection(QLoudness lLoudness)
    {
        qLoudness = lLoudness;
        QLoudnessSectionBody = QContract.QContractSheetFind<StackPanel>("PLoudness");
        QLoudnessSectionPersistent = QContract.QContractSheetFind<CheckBox>("PLoudnessPersistent");

        CheckBox lActive = QContract.QContractFind<CheckBox>(QLoudnessSectionBody, "PLoudnessActive");
        StackPanel lPanel = QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PLoudnessPanel");
        ComboBox lMode = QContract.QContractFind<ComboBox>(QLoudnessSectionBody, "PLoudnessMode");
        ComboBox lLoudnessPreset = QContract.QContractFind<ComboBox>(QLoudnessSectionBody, "PLoudnessPreset");
        ComboBox lDynamicPreset = QContract.QContractFind<ComboBox>(QLoudnessSectionBody, "PDynamicPreset");
        StackPanel lLoudnessRows = QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PLoudnessRows");
        StackPanel lDynamicRows = QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PDynamicRows");
        CheckBox lTwopass = QContract.QContractFind<CheckBox>(QLoudnessSectionBody, "PLoudnessTwopass");
        TextBlock lNotice = QContract.QContractFind<TextBlock>(QLoudnessSectionBody, "PLoudnessNotice");
        var lHandlers = new List<(Action, Action)>();

        lActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        lActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.ApplyTooltip");
        QLoudnessSectionPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QLoudnessSectionPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.PersistentTooltip");
        ((TextBlock)((Grid)lMode.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.Mode");
        string lPresetLabel = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset");
        ((TextBlock)((Grid)lLoudnessPreset.Parent).Children[0]).Text = lPresetLabel;
        ((TextBlock)((Grid)lDynamicPreset.Parent).Children[0]).Text = lPresetLabel;
        lTwopass.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.TwoPass");
        lTwopass.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.TwoPassTooltip");
        lNotice.Text = QLocalizationCatalog.QLocalizationTextRead("Inspector.Normalize.Notice");

        lHandlers.Add(QTool.QToolSwitchAttach(lActive, qLoudness.QLoudnessActiveSet));
        lHandlers.Add(QTool.QToolSwitchAttach(QLoudnessSectionPersistent, qLoudness.QLoudnessPersistentSet));
        lHandlers.Add(QTool.QToolSwitchAttach(lTwopass, qLoudness.QLoudnessTwopassSet));

        _ = new QLoudnessMode(qLoudness, lMode, lHandlers);
        _ = new QLoudnessChoice(qLoudness, false, lLoudnessPreset, lHandlers);
        _ = new QLoudnessChoice(qLoudness, true, lDynamicPreset, lHandlers);

        (StackPanel, bool)[] lRowGroups =
        {
            (lLoudnessRows, false),
            (lDynamicRows, true)
        };
        for (int lGroup = 0; lGroup < lRowGroups.Length; lGroup++)
        {
            (StackPanel lRows, bool lDynamic) = lRowGroups[lGroup];
            IReadOnlyList<QInspectorRow> lPlans = qLoudness.QLoudnessRowsRead(lDynamic);
            for (int lSlot = 0; lSlot < lRows.Children.Count; lSlot++)
            {
                int lIndex = lSlot;
                QInspectorRow lPlan = lPlans[lIndex];
                Grid lRow = (Grid)lRows.Children[lIndex];
                _ = new QLoudnessRow(qLoudness, lDynamic, lIndex, lPlan, lRow, lHandlers);
            }
        }

        QLoudnessSectionBody.Resources[typeof(QLoudnessSection)] = lHandlers;
        QLoudnessSectionAttach();
    }

    public StackPanel QLoudnessSectionBody { get; }

    public CheckBox QLoudnessSectionPersistent { get; }

    public void QLoudnessSectionAttach()
    {
        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)QLoudnessSectionBody.Resources[typeof(QLoudnessSection)])
        {
            lAttach();
        }

        qLoudness.QLoudnessChanged -= QLoudnessSectionUpdate;
        qLoudness.QLoudnessChanged += QLoudnessSectionUpdate;
    }

    public void QLoudnessSectionDetach()
    {
        qLoudness.QLoudnessChanged -= QLoudnessSectionUpdate;
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)QLoudnessSectionBody.Resources[typeof(QLoudnessSection)])
        {
            lDetach();
        }
    }

    public void QLoudnessSectionUpdate()
    {
        StackPanel lLoudnessRows = QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PLoudnessRows");
        StackPanel lDynamicRows = QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PDynamicRows");
        qLoudness.QLoudnessUpdate(
            QContract.QContractFind<CheckBox>(QLoudnessSectionBody, "PLoudnessActive"),
            QLoudnessSectionPersistent,
            QContract.QContractFind<CheckBox>(QLoudnessSectionBody, "PLoudnessTwopass"),
            QContract.QContractFind<ComboBox>(QLoudnessSectionBody, "PLoudnessMode"),
            [
                QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PLoudnessStack"),
                QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PDynamicStack")
            ],
            [
                QContract.QContractFind<Grid>(QLoudnessSectionBody, "PLoudnessPresetRow"),
                QContract.QContractFind<Grid>(QLoudnessSectionBody, "PDynamicPresetRow")
            ],
            [
                lLoudnessRows.Children.Cast<Grid>()
                    .Select(lRow => ((Slider)lRow.Children[1], (TextBox)lRow.Children[3]))
                    .ToList(),
                lDynamicRows.Children.Cast<Grid>()
                    .Select(lRow => ((Slider)lRow.Children[1], (TextBox)lRow.Children[3]))
                    .ToList()
            ],
            [
                QContract.QContractFind<ComboBox>(QLoudnessSectionBody, "PLoudnessPreset"),
                QContract.QContractFind<ComboBox>(QLoudnessSectionBody, "PDynamicPreset")
            ],
            QContract.QContractFind<StackPanel>(QLoudnessSectionBody, "PLoudnessPanel"));
    }

    private sealed class QLoudnessMode
    {
        public QLoudnessMode(
            QLoudness lDriver,
            ComboBox lMode,
            List<(Action, Action)> lHandlers)
        {
            QDropdown.QDropdownAttach(lMode);
            foreach (string lName in lDriver.QLoudnessModeNames)
            {
                lMode.Items.Add(lName);
            }

            SelectionChangedEventHandler lHandler = (_, _) => lDriver.QLoudnessModeSelect(lMode.SelectedIndex);
            Action lAttach = () =>
            {
                lMode.SelectionChanged -= lHandler;
                lMode.SelectionChanged += lHandler;
            };
            Action lDetach = () => lMode.SelectionChanged -= lHandler;
            lAttach();
            lHandlers.Add((lAttach, lDetach));
        }
    }

    private sealed class QLoudnessChoice
    {
        private readonly QLoudness qLoudnessChoiceDriver;
        private readonly bool qLoudnessChoiceDynamic;
        private readonly ComboBox qLoudnessChoicePreset;

        public QLoudnessChoice(
            QLoudness lDriver,
            bool lDynamic,
            ComboBox lPreset,
            List<(Action, Action)> lHandlers)
        {
            qLoudnessChoiceDriver = lDriver;
            qLoudnessChoiceDynamic = lDynamic;
            qLoudnessChoicePreset = lPreset;
            QDropdown.QDropdownAttach(lPreset);
            foreach (string lName in lDriver.QLoudnessChoiceRead(lDynamic).QInspectorChoiceNames)
            {
                lPreset.Items.Add(lName);
            }

            Action lAttach = () =>
            {
                lPreset.SelectionChanged -= QLoudnessChoiceSelect;
                lPreset.SelectionChanged += QLoudnessChoiceSelect;
            };
            Action lDetach = () => lPreset.SelectionChanged -= QLoudnessChoiceSelect;
            lAttach();
            lHandlers.Add((lAttach, lDetach));
        }

        private void QLoudnessChoiceSelect(object lSender, SelectionChangedEventArgs lChange) =>
            qLoudnessChoiceDriver.QLoudnessChoiceSelect(
                qLoudnessChoiceDynamic,
                qLoudnessChoicePreset.SelectedIndex);
    }

    private sealed class QLoudnessRow
    {
        private readonly QLoudness qLoudnessRowDriver;
        private readonly bool qLoudnessRowDynamic;
        private readonly int qLoudnessRowIndex;
        private readonly Slider qLoudnessRowSlider;

        public QLoudnessRow(
            QLoudness lDriver,
            bool lDynamic,
            int lIndex,
            QInspectorRow lPlan,
            Grid lRow,
            List<(Action, Action)> lHandlers)
        {
            qLoudnessRowDriver = lDriver;
            qLoudnessRowDynamic = lDynamic;
            qLoudnessRowIndex = lIndex;
            qLoudnessRowSlider = (Slider)lRow.Children[1];
            ((TextBlock)lRow.Children[0]).Text = lPlan.QInspectorRowLabel;
            ((TextBlock)lRow.Children[2]).Text = lPlan.QInspectorRowUnit;
            TextBox lNumber = (TextBox)lRow.Children[3];

            qLoudnessRowSlider.Minimum = lPlan.QInspectorRowLeast;
            qLoudnessRowSlider.Maximum = lPlan.QInspectorRowMost;
            QSlider.QSliderAttach(qLoudnessRowSlider);
            QSlider.QSliderResetAttach(qLoudnessRowSlider, QLoudnessRowReset);
            QTextbox.QTextboxAttach(lNumber);
            lNumber.PreviewTextInput +=
                (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
            Action lCommit = () =>
            {
                qLoudnessRowSlider.Value = QInspectorField.QInspectorValueCommit(
                    lNumber.Text,
                    qLoudnessRowSlider.Value,
                    qLoudnessRowSlider.Minimum,
                    qLoudnessRowSlider.Maximum);
                lNumber.CaretIndex = lNumber.Text.Length;
            };
            RoutedEventHandler lLost = (_, _) => lCommit();
            KeyEventHandler lKey = (_, lKey) =>
            {
                if (lKey.Key == Key.Enter)
                {
                    lCommit();
                }

                lKey.Handled = lKey.Key == Key.Enter;
            };
            Action lAttach = () =>
            {
                qLoudnessRowSlider.ValueChanged -= QLoudnessRowSet;
                lNumber.LostFocus -= lLost;
                lNumber.KeyDown -= lKey;
                qLoudnessRowSlider.ValueChanged += QLoudnessRowSet;
                lNumber.LostFocus += lLost;
                lNumber.KeyDown += lKey;
            };
            Action lDetach = () =>
            {
                qLoudnessRowSlider.ValueChanged -= QLoudnessRowSet;
                lNumber.LostFocus -= lLost;
                lNumber.KeyDown -= lKey;
            };
            lAttach();
            lHandlers.Add((lAttach, lDetach));
        }

        private double QLoudnessRowReset() =>
            qLoudnessRowDriver.QLoudnessDefaultRead(qLoudnessRowDynamic, qLoudnessRowIndex);

        private void QLoudnessRowSet(object lSender, RoutedPropertyChangedEventArgs<double> lChange) =>
            qLoudnessRowDriver.QLoudnessValueSet(
                qLoudnessRowDynamic,
                qLoudnessRowIndex,
                qLoudnessRowSlider.Value);
    }
}
