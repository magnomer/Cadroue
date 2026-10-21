using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

// The equalizer section owns its WPF contract, rows, and direct handoff of each user action to the driver.
public sealed class QEqualizerSection
{
    private readonly QEqualizer qEqualizer;

    public QEqualizerSection(QEqualizer lEqualizer)
    {
        qEqualizer = lEqualizer;
        QEqualizerSectionBody = QContract.QContractSheetFind<StackPanel>("PEqualizer");
        QEqualizerSectionPersistent = QContract.QContractSheetFind<CheckBox>("PEqualizerPersistent");

        CheckBox lActive = QContract.QContractFind<CheckBox>(QEqualizerSectionBody, "PEqualizerActive");
        ComboBox lPreset = QContract.QContractFind<ComboBox>(QEqualizerSectionBody, "PEqualizerPreset");
        Button lInsertion = QContract.QContractFind<Button>(QEqualizerSectionBody, "PEqualizerInsertion");

        lActive.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Apply");
        lActive.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.ApplyTooltip");
        QEqualizerSectionPersistent.Content = QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Persistent");
        QEqualizerSectionPersistent.ToolTip =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.PersistentTooltip");
        ((TextBlock)((Grid)lPreset.Parent).Children[0]).Text =
            QLocalizationCatalog.QLocalizationTextRead("Inspector.Common.Preset");
        lInsertion.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.Add");
        ((Image)lInsertion.Content).Source =
            QIcon.QIconRead("/PAsset/PPanel/PFunnelAdd.svg", QTool.QToolIconRead());

        QTool.QToolSwitchAttach(lActive, qEqualizer.QEqualizerActiveSet);
        QTool.QToolSwitchAttach(QEqualizerSectionPersistent, qEqualizer.QEqualizerPersistentSet);
        QDropdown.QDropdownAttach(lPreset);
        QButton.QButtonAttach(lInsertion, QButtonLook.QButtonLookPanel);
        foreach (string lName in qEqualizer.QEqualizerChoiceRead().QInspectorChoiceNames)
        {
            lPreset.Items.Add(lName);
        }

        lPreset.SelectionChanged += (_, _) => qEqualizer.QEqualizerChoiceSelect(lPreset.SelectedIndex);
        qEqualizer.QEqualizerRowsChanged += QEqualizerSectionRebuild;
        QEqualizerSectionRebuild();
    }

    public StackPanel QEqualizerSectionBody { get; }

    public CheckBox QEqualizerSectionPersistent { get; }

    public void QEqualizerSectionAttach()
    {
        QContract.QContractFind<Button>(QEqualizerSectionBody, "PEqualizerInsertion").Click +=
            (_, _) => qEqualizer.QEqualizerBandAdd();
        qEqualizer.QEqualizerChanged += QEqualizerSectionUpdate;
    }

    public void QEqualizerSectionUpdate()
    {
        CheckBox lActive = QContract.QContractFind<CheckBox>(QEqualizerSectionBody, "PEqualizerActive");
        ComboBox lPreset = QContract.QContractFind<ComboBox>(QEqualizerSectionBody, "PEqualizerPreset");
        StackPanel lRows = QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows");
        StackPanel lStack = QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerStack");

        QTool.QToolSwitchUpdate(lActive, qEqualizer.QEqualizerActive);
        QTool.QToolSwitchUpdate(QEqualizerSectionPersistent, qEqualizer.QEqualizerPersistent);
        qEqualizer.QEqualizerBandUpdate(lRows.Children.Cast<Grid>()
            .Select(lRow => (
                QContract.QContractFind<TextBox>(lRow, "PEqualizerFrequency"),
                QContract.QContractFind<Slider>(lRow, "PEqualizerSlider"),
                QContract.QContractFind<TextBox>(lRow, "PEqualizerValue")))
            .ToList());
        QInspectorChoice lChoice = qEqualizer.QEqualizerChoiceRead();
        lPreset.Items[lChoice.QInspectorChoiceCustom] = lChoice.QInspectorChoiceText;
        lPreset.SelectedIndex = lChoice.QInspectorChoiceIndex;
        lStack.IsEnabled = qEqualizer.QEqualizerActive;
    }

    private void QEqualizerSectionRebuild()
    {
        StackPanel lRows = QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows");
        lRows.Children.Clear();
        foreach (QEqualizerBand lBand in qEqualizer.QEqualizerRows)
        {
            (Grid, TextBox, Slider, TextBox) lRow = QEqualizerBandBuild(lBand);
            QEqualizerBandAttach(lRow);
            lRows.Children.Add(lRow.Item1);
        }
    }

    private (Grid, TextBox, Slider, TextBox) QEqualizerBandBuild(QEqualizerBand lBand)
    {
        var lRow = (Grid)QContract.QContractSheetFind<DataTemplate>(QEqualizerSectionBody, "pEqualizerBand")
            .LoadContent();
        TextBox lFrequency = QContract.QContractFind<TextBox>(lRow, "PEqualizerFrequency");
        Slider lSlider = QContract.QContractFind<Slider>(lRow, "PEqualizerSlider");
        TextBox lValue = QContract.QContractFind<TextBox>(lRow, "PEqualizerValue");
        Button lRemoval = QContract.QContractFind<Button>(lRow, "PEqualizerRemoval");

        lFrequency.Text = lBand.QEqualizerBandFrequency;
        lSlider.Minimum = qEqualizer.QEqualizerGainLeast;
        lSlider.Maximum = qEqualizer.QEqualizerGainMost;
        lSlider.Value = lBand.QEqualizerBandGain;
        lValue.Text = QInspectorField.QInspectorValueFormat(lValue.Text, lBand.QEqualizerBandGain, "0.#");
        lRemoval.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.Remove");
        ((Image)lRemoval.Content).Source =
            QIcon.QIconRead("/PAsset/PPanel/PFunnelRemove.svg", QTool.QToolIconRead());
        QButton.QButtonAttach(lRemoval, QButtonLook.QButtonLookPanel);
        lRemoval.Click += (_, _) => qEqualizer.QEqualizerBandRemove(
            QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows").Children.IndexOf(lRow));
        return (lRow, lFrequency, lSlider, lValue);
    }

    private void QEqualizerBandAttach((Grid, TextBox, Slider, TextBox) lBand)
    {
        (Grid lRow, TextBox lFrequency, Slider lSlider, TextBox lValue) = lBand;
        QSlider.QSliderAttach(lSlider);
        QSlider.QSliderResetAttach(lSlider, () => qEqualizer.QEqualizerGainOrigin);
        QTextbox.QTextboxAttach(lFrequency);
        QTextbox.QTextboxAttach(lValue);
        lFrequency.PreviewTextInput +=
            (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
        lValue.PreviewTextInput +=
            (_, lInput) => lInput.Handled = !QInspectorField.QInspectorDecimalCheck(lInput.Text);
        lSlider.ValueChanged += (_, _) => qEqualizer.QEqualizerGainSet(
            QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows").Children.IndexOf(lRow),
            lSlider.Value);
        lValue.LostFocus += (_, _) =>
        {
            lSlider.Value = QInspectorField.QInspectorValueCommit(
                lValue.Text,
                lSlider.Value,
                lSlider.Minimum,
                lSlider.Maximum);
            lValue.CaretIndex = lValue.Text.Length;
        };
        lValue.KeyDown += (_, lKey) =>
        {
            if (lKey.Key == Key.Enter)
            {
                lSlider.Value = QInspectorField.QInspectorValueCommit(
                    lValue.Text,
                    lSlider.Value,
                    lSlider.Minimum,
                    lSlider.Maximum);
                lValue.CaretIndex = lValue.Text.Length;
            }

            lKey.Handled = lKey.Key == Key.Enter;
        };
        lFrequency.LostFocus += (_, _) => QEqualizerFrequencyApply(lRow, lFrequency);
        lFrequency.KeyDown += (_, lKey) =>
        {
            if (lKey.Key == Key.Enter)
            {
                QEqualizerFrequencyApply(lRow, lFrequency);
            }

            lKey.Handled = lKey.Key == Key.Enter;
        };
    }

    private void QEqualizerFrequencyApply(Grid lRow, TextBox lFrequency)
    {
        lFrequency.Text = qEqualizer.QEqualizerFrequencyCommit(
            QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows").Children.IndexOf(lRow),
            lFrequency.Text);
        lFrequency.CaretIndex = lFrequency.Text.Length;
    }
}
