using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The noise reduction tool's wiring: paints the gate's values, type and preset rows onto the section's parts, and
// hands every input to the gate. Rows follow the gate's slots: reduction, smoothing, floor, residual, adaptivity.
public sealed class QNoise
{
    private static readonly string[] qNoiseLabelKeys =
    {
        "Inspector.Common.Amount",
        "Inspector.Noise.Smoothing",
        "Inspector.Noise.Floor",
        "Inspector.Noise.Residual",
        "Inspector.Noise.Adaptivity"
    };

    private static readonly string[] qNoiseUnits = { "dB", "gs", "dB", "dB", "0-1" };

    private static readonly string[] qNoisePatterns = { "0.#", "0.#", "0.#", "0.#", "0.###" };

    private readonly CNoise qNoiseGate;

    public QNoise(CNoise lGate)
    {
        qNoiseGate = lGate;
    }

    public event Action? QNoiseChanged
    {
        add => qNoiseGate.CNoiseChanged += value;
        remove => qNoiseGate.CNoiseChanged -= value;
    }

    public bool QNoisePersistent => qNoiseGate.CNoisePersistent;

    public IReadOnlyList<string> QNoiseTypeNames => qNoiseGate.CNoiseTypes
        .Select(static lRow => QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
        .ToList();

    public IReadOnlyList<QInspectorRow> QNoiseRows => Enumerable.Range(0, qNoiseLabelKeys.Length)
        .Select(lSlot => QInspectorPlan.QInspectorRowCreate(
            lSlot,
            qNoiseLabelKeys[lSlot],
            qNoiseUnits[lSlot],
            qNoisePatterns[lSlot],
            qNoiseGate.CNoiseLeasts[lSlot],
            qNoiseGate.CNoiseMosts[lSlot]))
        .ToList();

    // The combo shows each row's wording and selects the row the gate marked chosen.
    public QInspectorChoice QNoiseChoiceRead()
    {
        IReadOnlyList<CChoice> lRows = qNoiseGate.CNoiseChoices;
        List<string> lNames = lRows
            .Select(static lRow => lRow.CChoiceBasis is { } lBasis
                ? QLocalizationCatalog.QLocalizationFormat(
                    lRow.CChoiceKey, QLocalizationCatalog.QLocalizationTextRead(lBasis))
                : QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
            .ToList();
        int lCustom = lRows.Count - 1;
        int lChosen = lRows.ToList().FindIndex(lRow => lRow.CChoiceChosen);
        return new QInspectorChoice(lNames, lChosen, lCustom, lNames[lCustom]);
    }

    public double QNoiseValueRead(int lRow) => qNoiseGate.CNoiseValueRead(lRow);

    public double QNoiseDefaultRead(int lRow) => qNoiseGate.CNoiseDefaultRead(lRow);

    public void QNoiseValueSet(int lRow, double lValue) => qNoiseGate.CNoiseValueSet(lRow, lValue);

    public void QNoiseActiveSet(bool lActive) => qNoiseGate.CNoiseActiveSet(lActive);

    public void QNoiseTrackSet(bool lTrack) => qNoiseGate.CNoiseTrackSet(lTrack);

    public void QNoisePersistentSet(bool lPersistent) => qNoiseGate.CNoisePersistentSet(lPersistent);

    public void QNoiseTypeSelect(int lIndex) => qNoiseGate.CNoiseTypeSelect(lIndex);

    public void QNoiseChoiceSelect(int lIndex) => qNoiseGate.CNoiseChoiceSelect(lIndex);

    // The section's parts take the gate's state: the switches, each row's slider and number, the type and the preset
    // combo, then the stack is lit while the tool applies. Rows come in slot order.
    public void QNoiseUpdate(
        CheckBox lApply,
        CheckBox lPersistent,
        CheckBox lTrack,
        IReadOnlyList<(Slider QNoiseSlider, TextBox QNoiseValue)> lRows,
        ComboBox lType,
        ComboBox lPreset,
        FrameworkElement lStack)
    {
        QTool.QToolSwitchUpdate(lApply, qNoiseGate.CNoiseActive);
        QTool.QToolSwitchUpdate(lPersistent, qNoiseGate.CNoisePersistent);
        QTool.QToolSwitchUpdate(lTrack, qNoiseGate.CNoiseTrack);
        for (int lRow = 0; lRow < lRows.Count; lRow++)
        {
            QNoiseRowUpdate(lRows[lRow], qNoiseGate.CNoiseValueRead(lRow), qNoisePatterns[lRow]);
        }

        lType.SelectedIndex = qNoiseGate.CNoiseTypes.ToList().FindIndex(lRow => lRow.CChoiceChosen);
        QInspectorChoice lChoice = QNoiseChoiceRead();
        lPreset.Items[lChoice.QInspectorChoiceCustom] = lChoice.QInspectorChoiceText;
        lPreset.SelectedIndex = lChoice.QInspectorChoiceIndex;
        QTool.QToolStackUpdate(lStack, qNoiseGate.CNoiseActive);
    }

    private static void QNoiseRowUpdate(
        (Slider QNoiseSlider, TextBox QNoiseValue) lRow, double lNumber, string lPattern)
    {
        lRow.QNoiseSlider.Value = Math.Clamp(lNumber, lRow.QNoiseSlider.Minimum, lRow.QNoiseSlider.Maximum);
        lRow.QNoiseValue.Text = QInspector.QInspectorValueFormat(lRow.QNoiseValue.Text, lNumber, lPattern);
    }
}
