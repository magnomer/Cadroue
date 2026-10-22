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
        var lHandlers = new List<(Action, Action)>();

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

        lHandlers.Add(QTool.QToolSwitchAttach(lActive, qEqualizer.QEqualizerActiveSet));
        lHandlers.Add(QTool.QToolSwitchAttach(QEqualizerSectionPersistent, qEqualizer.QEqualizerPersistentSet));
        QDropdown.QDropdownAttach(lPreset);
        QButton.QButtonAttach(lInsertion, QButtonLook.QButtonLookPanel);
        foreach (string lName in qEqualizer.QEqualizerChoiceRead().QInspectorChoiceNames)
        {
            lPreset.Items.Add(lName);
        }

        SelectionChangedEventHandler lPresetChanged =
            (_, _) => qEqualizer.QEqualizerChoiceSelect(lPreset.SelectedIndex);
        RoutedEventHandler lInsert = (_, _) => qEqualizer.QEqualizerBandAdd();
        Action lAttach = () =>
        {
            lPreset.SelectionChanged -= lPresetChanged;
            lInsertion.Click -= lInsert;
            lPreset.SelectionChanged += lPresetChanged;
            lInsertion.Click += lInsert;
        };
        Action lDetach = () =>
        {
            lPreset.SelectionChanged -= lPresetChanged;
            lInsertion.Click -= lInsert;
        };
        lAttach();
        lHandlers.Add((lAttach, lDetach));
        QEqualizerSectionBody.Resources[typeof(QEqualizerSection)] = lHandlers;
        QEqualizerSectionBody.Resources[typeof(QEqualizerBand)] = new List<(Action, Action)>();
        QEqualizerSectionBody.Resources[typeof(QInspectorFrame)] = false;
        QEqualizerSectionAttach();
        QEqualizerSectionRebuild();
    }

    public StackPanel QEqualizerSectionBody { get; }

    public CheckBox QEqualizerSectionPersistent { get; }

    public void QEqualizerSectionAttach()
    {
        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)QEqualizerSectionBody.Resources[typeof(QEqualizerSection)])
        {
            lAttach();
        }

        foreach ((Action lAttach, _) in
                 (List<(Action, Action)>)QEqualizerSectionBody.Resources[typeof(QEqualizerBand)])
        {
            lAttach();
        }

        qEqualizer.QEqualizerRowsChanged -= QEqualizerSectionRebuild;
        qEqualizer.QEqualizerChanged -= QEqualizerSectionUpdate;
        qEqualizer.QEqualizerRowsChanged += QEqualizerSectionRebuild;
        qEqualizer.QEqualizerChanged += QEqualizerSectionUpdate;
        QEqualizerSectionBody.Resources[typeof(QInspectorFrame)] = true;
    }

    public void QEqualizerSectionDetach()
    {
        QEqualizerSectionBody.Resources[typeof(QInspectorFrame)] = false;
        qEqualizer.QEqualizerRowsChanged -= QEqualizerSectionRebuild;
        qEqualizer.QEqualizerChanged -= QEqualizerSectionUpdate;
        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)QEqualizerSectionBody.Resources[typeof(QEqualizerSection)])
        {
            lDetach();
        }

        foreach ((_, Action lDetach) in
                 (List<(Action, Action)>)QEqualizerSectionBody.Resources[typeof(QEqualizerBand)])
        {
            lDetach();
        }
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
        var lHandlers =
            (List<(Action, Action)>)QEqualizerSectionBody.Resources[typeof(QEqualizerBand)];
        foreach ((_, Action lDetach) in lHandlers)
        {
            lDetach();
        }

        lHandlers.Clear();
        lRows.Children.Clear();
        foreach (QEqualizerBand lBand in qEqualizer.QEqualizerRows)
        {
            (Grid, TextBox, Slider, TextBox) lRow = QEqualizerBandBuild(lBand, lHandlers);
            QEqualizerBandAttach(lRow, lHandlers);
            lRows.Children.Add(lRow.Item1);
        }
    }

    private (Grid, TextBox, Slider, TextBox) QEqualizerBandBuild(
        QEqualizerBand lBand,
        List<(Action, Action)> lHandlers)
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
        RoutedEventHandler lRemove = (_, _) => qEqualizer.QEqualizerBandRemove(
            QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows").Children.IndexOf(lRow));
        Action lAttach = () =>
        {
            lRemoval.Click -= lRemove;
            lRemoval.Click += lRemove;
        };
        Action lDetach = () => lRemoval.Click -= lRemove;
        if ((bool)QEqualizerSectionBody.Resources[typeof(QInspectorFrame)])
        {
            lAttach();
        }

        lHandlers.Add((lAttach, lDetach));
        return (lRow, lFrequency, lSlider, lValue);
    }

    private void QEqualizerBandAttach(
        (Grid, TextBox, Slider, TextBox) lBand,
        List<(Action, Action)> lHandlers)
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
        RoutedPropertyChangedEventHandler<double> lSlide = (_, _) => qEqualizer.QEqualizerGainSet(
            QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows").Children.IndexOf(lRow),
            lSlider.Value);
        RoutedEventHandler lValueLost = (_, _) =>
        {
            lSlider.Value = QInspectorField.QInspectorValueCommit(
                lValue.Text,
                lSlider.Value,
                lSlider.Minimum,
                lSlider.Maximum);
            lValue.CaretIndex = lValue.Text.Length;
        };
        KeyEventHandler lValueKey = (_, lKey) =>
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
        RoutedEventHandler lFrequencyLost = (_, _) => QEqualizerFrequencyApply(lRow, lFrequency);
        KeyEventHandler lFrequencyKey = (_, lKey) =>
        {
            if (lKey.Key == Key.Enter)
            {
                QEqualizerFrequencyApply(lRow, lFrequency);
            }

            lKey.Handled = lKey.Key == Key.Enter;
        };
        Action lAttach = () =>
        {
            lSlider.ValueChanged -= lSlide;
            lValue.LostFocus -= lValueLost;
            lValue.KeyDown -= lValueKey;
            lFrequency.LostFocus -= lFrequencyLost;
            lFrequency.KeyDown -= lFrequencyKey;
            lSlider.ValueChanged += lSlide;
            lValue.LostFocus += lValueLost;
            lValue.KeyDown += lValueKey;
            lFrequency.LostFocus += lFrequencyLost;
            lFrequency.KeyDown += lFrequencyKey;
        };
        Action lDetach = () =>
        {
            lSlider.ValueChanged -= lSlide;
            lValue.LostFocus -= lValueLost;
            lValue.KeyDown -= lValueKey;
            lFrequency.LostFocus -= lFrequencyLost;
            lFrequency.KeyDown -= lFrequencyKey;
        };
        if ((bool)QEqualizerSectionBody.Resources[typeof(QInspectorFrame)])
        {
            lAttach();
        }

        lHandlers.Add((lAttach, lDetach));
    }

    private void QEqualizerFrequencyApply(Grid lRow, TextBox lFrequency)
    {
        lFrequency.Text = qEqualizer.QEqualizerFrequencyCommit(
            QContract.QContractFind<StackPanel>(QEqualizerSectionBody, "PEqualizerRows").Children.IndexOf(lRow),
            lFrequency.Text);
        lFrequency.CaretIndex = lFrequency.Text.Length;
    }
}
