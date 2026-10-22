using Cadroue.Conduct;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed record QEqualizerBand(int QEqualizerBandIndex, string QEqualizerBandFrequency, double QEqualizerBandGain);

// The equalizer tool's wiring: paints the gate's bands and preset rows, and hands every input to the gate.
public sealed class QEqualizer
{
    private readonly CEqualizer qEqualizerGate;

    public QEqualizer(CEqualizer lGate)
    {
        qEqualizerGate = lGate;
    }

    public event Action? QEqualizerChanged
    {
        add => qEqualizerGate.CEqualizerChanged += value;
        remove => qEqualizerGate.CEqualizerChanged -= value;
    }

    public event Action? QEqualizerRowsChanged
    {
        add => qEqualizerGate.CEqualizerRowsChanged += value;
        remove => qEqualizerGate.CEqualizerRowsChanged -= value;
    }

    public bool QEqualizerActive => qEqualizerGate.CEqualizerActive;

    public bool QEqualizerPersistent => qEqualizerGate.CEqualizerPersistent;

    public double QEqualizerGainLeast => qEqualizerGate.CEqualizerGainLeast;

    public double QEqualizerGainMost => qEqualizerGate.CEqualizerGainMost;

    public double QEqualizerGainOrigin => qEqualizerGate.CEqualizerGainOrigin;

    public IReadOnlyList<QEqualizerBand> QEqualizerRows => qEqualizerGate.CEqualizerRows
        .Select(lRow => new QEqualizerBand(
            lRow.CEqualizerRowIndex,
            lRow.CEqualizerRowFrequency,
            lRow.CEqualizerRowGain))
        .ToList();

    // The combo shows each row's wording and selects the row the gate marked chosen.
    public QInspectorChoice QEqualizerChoiceRead() =>
        QInspectorPlan.QInspectorChoiceBuild(CChoicePlan.CChoicePlanBuild(qEqualizerGate.CEqualizerChoices));

    internal void QEqualizerBandUpdate(IReadOnlyList<(TextBox, Slider, TextBox)> lRows)
    {
        IReadOnlyList<QEqualizerBand> lBands = QEqualizerRows;
        for (int lIndex = 0; lIndex < lRows.Count; lIndex++)
        {
            (TextBox lFrequency, Slider lSlider, TextBox lValue) = lRows[lIndex];
            QEqualizerBand lBand = lBands[lIndex];
            lFrequency.Text = lBand.QEqualizerBandFrequency;
            lSlider.Minimum = qEqualizerGate.CEqualizerGainLeast;
            lSlider.Maximum = qEqualizerGate.CEqualizerGainMost;
            QInspectorField.QInspectorFieldUpdate(lSlider, lValue, lBand.QEqualizerBandGain, "0.#");
        }
    }

    public string QEqualizerFrequencyRead(int lIndex) => qEqualizerGate.CEqualizerFrequencyRead(lIndex);

    public string QEqualizerFrequencyCommit(int lIndex, string lText) =>
        qEqualizerGate.CEqualizerFrequencyCommit(lIndex, lText);

    public void QEqualizerGainSet(int lIndex, double lGain) => qEqualizerGate.CEqualizerGainSet(lIndex, lGain);

    public void QEqualizerChoiceSelect(int lIndex) => qEqualizerGate.CEqualizerChoiceSelect(lIndex);

    public void QEqualizerActiveSet(bool lActive) => qEqualizerGate.CEqualizerActiveSet(lActive);

    public void QEqualizerBandAdd() => qEqualizerGate.CEqualizerBandAdd();

    public void QEqualizerBandRemove(int lIndex) => qEqualizerGate.CEqualizerBandRemove(lIndex);

    public void QEqualizerPersistentSet(bool lPersistent) => qEqualizerGate.CEqualizerPersistentSet(lPersistent);
}
