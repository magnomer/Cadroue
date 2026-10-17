using System.Windows;
using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// One pass filter section's wiring: paints the gate's values, poles and preset rows onto the section's parts, and
// hands every input to the gate. Rows follow the gate's slots: frequency, stages, resonance.
public sealed class QFilter
{
    private static readonly string[] qFilterPatterns = { "0", "0", "0.###" };

    private readonly CFilter qFilterGate;

    public QFilter(CFilter lGate)
    {
        qFilterGate = lGate;
    }

    public event Action? QFilterChanged
    {
        add => qFilterGate.CFilterChanged += value;
        remove => qFilterGate.CFilterChanged -= value;
    }

    public IReadOnlyList<double> QFilterLeasts => qFilterGate.CFilterLeasts;

    public IReadOnlyList<double> QFilterMosts => qFilterGate.CFilterMosts;

    // Each preset row shows its wording; the custom row's wording takes the preset it drifted from. A broken
    // translation of that wording is traced by the catalog and shown in English or raw, never thrown.
    public IReadOnlyList<string> QFilterChoiceNames => qFilterGate.CFilterChoices
        .Select(static lRow => lRow.CChoiceBasis is { } lBasis
            ? QLocalizationCatalog.QLocalizationFormat(
                lRow.CChoiceKey, QLocalizationCatalog.QLocalizationTextRead(lBasis))
            : QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
        .ToList();

    public IReadOnlyList<string> QFilterPoleNames => qFilterGate.CFilterPoleChoices
        .Select(static lRow => QLocalizationCatalog.QLocalizationTextRead(lRow.CChoiceKey))
        .ToList();

    public double QFilterValueRead(int lSlot) => qFilterGate.CFilterValueRead(lSlot);

    public double QFilterDefaultRead(int lSlot) => qFilterGate.CFilterDefaultRead(lSlot);

    public void QFilterValueSet(int lSlot, double lValue) => qFilterGate.CFilterValueSet(lSlot, lValue);

    public void QFilterActiveSet(bool lActive) => qFilterGate.CFilterActiveSet(lActive);

    public void QFilterPersistentSet(bool lPersistent) => qFilterGate.CFilterPersistentSet(lPersistent);

    public void QFilterPolesSelect(int lIndex) => qFilterGate.CFilterPolesSelect(lIndex);

    public void QFilterChoiceSelect(int lIndex) => qFilterGate.CFilterChoiceSelect(lIndex);

    // The section's parts take the gate's state: the switches, each row's slider and number, the poles, the resonance
    // row's look, the preset combo, then the stack is lit while the filter applies. Rows come in slot order; the custom
    // row is the gate's last row and the combo's last item.
    public void QFilterUpdate(
        CheckBox lApply,
        CheckBox lPersistent,
        IReadOnlyList<(Slider QFilterSlider, TextBox QFilterValue)> lRows,
        ComboBox lPoles,
        FrameworkElement lResonanceRow,
        ComboBox lPreset,
        FrameworkElement lStack)
    {
        QTool.QToolSwitchUpdate(lApply, qFilterGate.CFilterActive);
        QTool.QToolSwitchUpdate(lPersistent, qFilterGate.CFilterPersistent);
        for (int lRow = 0; lRow < lRows.Count; lRow++)
        {
            QFilterRowUpdate(lRows[lRow], qFilterGate.CFilterValueRead(lRow), qFilterPatterns[lRow]);
        }

        lPoles.SelectedIndex = qFilterGate.CFilterPoleChoices.ToList().FindIndex(lRow => lRow.CChoiceChosen);
        QTool.QToolStackUpdate(lResonanceRow, qFilterGate.CFilterResonanceActive);
        lPreset.Items[lPreset.Items.Count - 1] = QFilterChoiceNames[^1];
        lPreset.SelectedIndex = qFilterGate.CFilterChoices.ToList().FindIndex(lRow => lRow.CChoiceChosen);
        QTool.QToolStackUpdate(lStack, qFilterGate.CFilterActive);
    }

    private static void QFilterRowUpdate(
        (Slider QFilterSlider, TextBox QFilterValue) lRow, double lNumber, string lPattern)
    {
        lRow.QFilterSlider.Value = Math.Clamp(lNumber, lRow.QFilterSlider.Minimum, lRow.QFilterSlider.Maximum);
        lRow.QFilterValue.Text = QInspector.QInspectorValueFormat(lRow.QFilterValue.Text, lNumber, lPattern);
    }
}
