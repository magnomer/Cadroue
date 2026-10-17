using System.Globalization;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed record QEqualizerBand(int QEqualizerBandIndex, string QEqualizerBandFrequency, double QEqualizerBandGain);

// The equalizer tool's wiring: paints the gate's bands and preset rows, and hands every input to the gate.
public sealed class QEqualizer
{
    private const string QEqualizerFrequencyPattern = "0.###";

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

    public string QEqualizerRemoveTip => QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.Remove");

    public IReadOnlyList<QEqualizerBand> QEqualizerRows => qEqualizerGate.CEqualizerBands
        .Select((lBand, lIndex) => new QEqualizerBand(
            lIndex,
            lBand.CEqualizerFrequency.ToString(QEqualizerFrequencyPattern, CultureInfo.InvariantCulture),
            lBand.CEqualizerGain))
        .ToList();

    // The combo shows each row's wording and selects the row the gate marked chosen.
    public QInspectorChoice QEqualizerChoiceRead()
    {
        IReadOnlyList<CChoice> lRows = qEqualizerGate.CEqualizerChoices;
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

    public double QEqualizerGainRead(int lIndex) => qEqualizerGate.CEqualizerGainRead(lIndex);

    public string QEqualizerFrequencyRead(int lIndex) => qEqualizerGate.CEqualizerFrequencyRead(lIndex)
        .ToString(QEqualizerFrequencyPattern, CultureInfo.InvariantCulture);

    public string QEqualizerFrequencyCommit(int lIndex, string lText) =>
        QInspector.QInspectorValueFormat(
            lText, qEqualizerGate.CEqualizerFrequencyCommit(lIndex, lText), QEqualizerFrequencyPattern);

    public void QEqualizerGainSet(int lIndex, double lGain) => qEqualizerGate.CEqualizerGainSet(lIndex, lGain);

    public void QEqualizerChoiceSelect(int lIndex) => qEqualizerGate.CEqualizerChoiceSelect(lIndex);

    public void QEqualizerActiveSet(bool lActive) => qEqualizerGate.CEqualizerActiveSet(lActive);

    public void QEqualizerBandAdd() => qEqualizerGate.CEqualizerBandAdd();

    public void QEqualizerBandRemove(int lIndex) => qEqualizerGate.CEqualizerBandRemove(lIndex);

    public void QEqualizerPersistentSet(bool lPersistent) => qEqualizerGate.CEqualizerPersistentSet(lPersistent);
}
