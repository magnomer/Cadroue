using System.Globalization;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QEqualizerBand(int QEqualizerBandIndex, string QEqualizerBandFrequency, double QEqualizerBandGain);

public sealed class QEqualizer
{
    private const string QEqualizerFrequencyPattern = "0.###";

    private bool qEqualizerActive;
    private List<LWorkBand> qEqualizerBands = LWorkEqualizerStep.LWorkBandsCreate().ToList();
    private string? qEqualizerToken;
    private bool qEqualizerPersistent;

    public QEqualizer()
    {
        qEqualizerToken = QEqualizerMatchRead();
    }

    public event Action? QEqualizerChanged;
    public event Action? QEqualizerRowsChanged;

    public bool QEqualizerActive => qEqualizerActive;

    public IReadOnlyList<LWorkBand> QEqualizerBands => qEqualizerBands;

    public string? QEqualizerToken => qEqualizerToken;

    public bool QEqualizerPersistent => qEqualizerPersistent;

    public double QEqualizerGainLeast => LContourCatalog.LContourGainLeast;

    public double QEqualizerGainMost => LContourCatalog.LContourGainMost;

    public string QEqualizerRemoveTip => QLocalizationCatalog.QLocalizationTextRead("Inspector.Equalizer.Remove");

    public IReadOnlyList<QEqualizerBand> QEqualizerRows => qEqualizerBands
        .Select((lBand, lIndex) => new QEqualizerBand(
            lIndex,
            lBand.LWorkBandFrequency.ToString(QEqualizerFrequencyPattern, CultureInfo.InvariantCulture),
            lBand.LWorkBandGain))
        .ToList();

    public QInspectorChoice QEqualizerChoiceRead() => QInspectorPlan.QInspectorChoiceRead(
        LContourCatalog.LContourTokensRead(), QEqualizerKeyRead, qEqualizerToken, QEqualizerMatchRead());

    public static string QEqualizerKeyRead(string lToken) => lToken switch
    {
        "Flat" => "Inspector.Equalizer.Preset.Flat",
        "Bass boost" => "Inspector.Equalizer.Preset.BassBoost",
        "Bright" => "Inspector.Equalizer.Preset.Bright",
        "Warm" => "Inspector.Equalizer.Preset.Warm",
        "Loudness" => "Inspector.Equalizer.Preset.Loudness",
        "Vocal" => "Inspector.Equalizer.Preset.Vocal",
        "De-ess" => "Inspector.Equalizer.Preset.Deess",
        "Podcast" => "Inspector.Equalizer.Preset.Podcast",
        "Telephone" => "Inspector.Equalizer.Preset.Telephone",
        _ => "Inspector.Common.Custom"
    };

    public double QEqualizerGainRead(int lIndex) =>
        lIndex >= 0 && lIndex < qEqualizerBands.Count ? qEqualizerBands[lIndex].LWorkBandGain : 0;

    public string QEqualizerFrequencyRead(int lIndex) =>
        QEqualizerFrequencyResolve(lIndex).ToString(QEqualizerFrequencyPattern, CultureInfo.InvariantCulture);

    public string QEqualizerFrequencyCommit(int lIndex, string lText)
    {
        double lCurrent = QEqualizerFrequencyResolve(lIndex);
        double lFrequency = QInspector.QInspectorValueCommit(lText, lCurrent, null, null);
        QEqualizerBandSet(lIndex, lFrequency, QEqualizerGainRead(lIndex));
        return QInspector.QInspectorValueFormat(lText, QEqualizerFrequencyResolve(lIndex), QEqualizerFrequencyPattern);
    }

    public void QEqualizerGainSet(int lIndex, double lGain) =>
        QEqualizerBandSet(lIndex, QEqualizerFrequencyResolve(lIndex), lGain);

    public void QEqualizerChoiceSelect(int lIndex)
    {
        string? lToken = QInspectorPlan.QInspectorChoiceResolve(
            LContourCatalog.LContourTokensRead(), lIndex, qEqualizerToken, QEqualizerMatchRead());
        if (lToken is not null)
        {
            QEqualizerPresetSelect(lToken);
        }
    }

    public LWorkAudioStep QEqualizerStepRead() =>
        LWorkAudioStep.LWorkEqualizerCreate(qEqualizerActive, qEqualizerBands.ToArray());

    public string? QEqualizerMatchRead() => LContourCatalog.LContourPresetFind(
        qEqualizerBands.Select(lBand => lBand.LWorkBandFrequency).ToArray(),
        qEqualizerBands.Select(lBand => lBand.LWorkBandGain).ToArray());

    public void QEqualizerStepSet(LWorkAudioStep lStep) =>
        QEqualizerBandsApply(lStep.LWorkStepActive, QEqualizerBandsResolve(lStep), null);

    // A step that is not an equalizer carries the default bands.
    private static List<LWorkBand> QEqualizerBandsResolve(LWorkAudioStep lStep)
    {
        IReadOnlyList<LWorkBand> lBands = lStep is LWorkEqualizerStep lEqualizer
            ? lEqualizer.LWorkEqualizerBands
            : LWorkEqualizerStep.LWorkBandsCreate();
        return lBands.ToList();
    }

    public void QEqualizerActiveSet(bool lActive)
    {
        if (qEqualizerActive == lActive)
        {
            return;
        }

        qEqualizerActive = lActive;
        QEqualizerChanged?.Invoke();
    }

    public void QEqualizerBandSet(int lIndex, double lFrequency, double lGain)
    {
        if (QEqualizerReplacedResolve(lIndex, lFrequency, lGain) is { } lBands)
        {
            QEqualizerBandsApply(qEqualizerActive, lBands, qEqualizerToken);
        }
    }

    public void QEqualizerBandAdd() =>
        QEqualizerBandsApply(qEqualizerActive, QEqualizerAddedResolve(), qEqualizerToken);

    public void QEqualizerBandRemove(int lIndex)
    {
        if (QEqualizerRemovedResolve(lIndex) is { } lBands)
        {
            QEqualizerBandsApply(qEqualizerActive, lBands, qEqualizerToken);
        }
    }

    public void QEqualizerPresetSelect(string lToken)
    {
        if (QEqualizerPresetResolve(lToken) is { } lBands)
        {
            QEqualizerBandsApply(qEqualizerActive, lBands, lToken);
        }
    }

    // The bands with one band replaced by its normal form; null for an index out of range or an unchanged band.
    private List<LWorkBand>? QEqualizerReplacedResolve(int lIndex, double lFrequency, double lGain)
    {
        if (lIndex < 0 || lIndex >= qEqualizerBands.Count)
        {
            return null;
        }

        LWorkBand lBand = QEqualizerNormalize(lFrequency, lGain);
        if (qEqualizerBands[lIndex] == lBand)
        {
            return null;
        }

        List<LWorkBand> lBands = qEqualizerBands.ToList();
        lBands[lIndex] = lBand;
        return lBands;
    }

    private List<LWorkBand> QEqualizerAddedResolve()
    {
        List<LWorkBand> lBands = qEqualizerBands.ToList();
        lBands.Add(new LWorkBand(LContourCatalog.LContourFrequencyDefault, 0));
        return lBands;
    }

    private List<LWorkBand>? QEqualizerRemovedResolve(int lIndex)
    {
        if (lIndex < 0 || lIndex >= qEqualizerBands.Count)
        {
            return null;
        }

        List<LWorkBand> lBands = qEqualizerBands.ToList();
        lBands.RemoveAt(lIndex);
        return lBands;
    }

    // The preset's gains laid on the band grid; null for an unknown preset.
    private static List<LWorkBand>? QEqualizerPresetResolve(string lToken)
    {
        if (LContourCatalog.LContourGainsRead(lToken) is not { } lGains)
        {
            return null;
        }

        double[] lGrid = LContourCatalog.LContourBandGrid;
        var lBands = new List<LWorkBand>();
        for (int lIndex = 0; lIndex < lGrid.Length; lIndex++)
        {
            lBands.Add(new LWorkBand(lGrid[lIndex], lGains[lIndex]));
        }

        return lBands;
    }

    public void QEqualizerPersistentSet(bool lPersistent)
    {
        if (qEqualizerPersistent == lPersistent)
        {
            return;
        }

        qEqualizerPersistent = lPersistent;
        QEqualizerChanged?.Invoke();
    }

    private double QEqualizerFrequencyResolve(int lIndex) =>
        lIndex >= 0 && lIndex < qEqualizerBands.Count
            ? qEqualizerBands[lIndex].LWorkBandFrequency
            : LContourCatalog.LContourFrequencyDefault;

    private static LWorkBand QEqualizerNormalize(double lFrequency, double lGain) =>
        ((LWorkEqualizerStep)LWorkAudioStep.LWorkEqualizerCreate(false, new[] { new LWorkBand(lFrequency, lGain) }))
            .LWorkEqualizerBands[0];

    private void QEqualizerBandsApply(bool lActive, List<LWorkBand> lBands, string? lFallback)
    {
        string? lToken = LContourCatalog.LContourPresetFind(
            lBands.Select(lBand => lBand.LWorkBandFrequency).ToArray(),
            lBands.Select(lBand => lBand.LWorkBandGain).ToArray()) ?? lFallback;
        if (qEqualizerActive == lActive && qEqualizerBands.SequenceEqual(lBands) && qEqualizerToken == lToken)
        {
            return;
        }

        bool lRows = qEqualizerBands.Count != lBands.Count;
        qEqualizerActive = lActive;
        qEqualizerBands = lBands;
        qEqualizerToken = lToken;
        if (lRows)
        {
            QEqualizerRowsChanged?.Invoke();
        }

        QEqualizerChanged?.Invoke();
    }
}
