using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum QSDiagnosisState
{
    QSDiagnosisStateChecking,
    QSDiagnosisStateReady,
    QSDiagnosisStateMissing
}

public enum QSDiagnosisMood
{
    QSDiagnosisMoodChecking,
    QSDiagnosisMoodReady,
    QSDiagnosisMoodWarning,
    QSDiagnosisMoodMissing,
    QSDiagnosisMoodAbsent
}

public sealed record QSDiagnosisItem(string QSDiagnosisLabel, string[] QSDiagnosisFilters);

public sealed class QSDiagnosis
{
    private static readonly QSDiagnosisItem[] qsDiagnosisVideoItems =
    [
        new("Diagnosis.Feature.Brightness", ["eq"]),
        new("Diagnosis.Feature.Contrast", ["eq"]),
        new("Diagnosis.Feature.Gamma", ["eq"]),
        new("Diagnosis.Feature.Saturation", ["eq"]),
        new("Diagnosis.Feature.Exposure", ["exposure", "scale", "format"]),
        new("Diagnosis.Feature.Whitebalance", ["colorcorrect", "scale", "format"]),
        new("Diagnosis.Feature.WhitebalanceManual", ["colorchannelmixer", "eq", "scale", "format"]),
        new("Diagnosis.Feature.Crop", ["crop"]),
        new("Diagnosis.Feature.Rotate", ["transpose", "hflip", "vflip"]),
        new("Diagnosis.Feature.Resize", ["scale"])
    ];

    private static readonly QSDiagnosisItem[] qsDiagnosisAudioItems =
    [
        new("Diagnosis.Feature.Volume", ["volume"]),
        new("Diagnosis.Feature.Loudness", ["loudnorm"]),
        new("Diagnosis.Feature.Dynamic", ["dynaudnorm"]),
        new("Diagnosis.Feature.Equalizer", ["equalizer"]),
        new("Diagnosis.Feature.Highpass", ["highpass"]),
        new("Diagnosis.Feature.Lowpass", ["lowpass"]),
        new("Diagnosis.Feature.Noise", ["afftdn"])
    ];

    private int qsDiagnosisGeneration;
    private LInventoryFeature? qsDiagnosisFeature;

    public event Action? QSDiagnosisChanged;

    public static IReadOnlyList<QSDiagnosisItem> QSDiagnosisVideoItems => qsDiagnosisVideoItems;

    public static IReadOnlyList<QSDiagnosisItem> QSDiagnosisAudioItems => qsDiagnosisAudioItems;

    public string QSDiagnosisTitle => QLocalizationCatalog.QLocalizationTextRead("Diagnosis.Window.Title");

    public bool QSDiagnosisChecking => qsDiagnosisFeature is null;

    public bool QSDiagnosisReady => !string.IsNullOrWhiteSpace(qsDiagnosisFeature?.LInventoryVersion);

    public string QSDiagnosisVersion => qsDiagnosisFeature?.LInventoryVersion ?? string.Empty;

    public string QSDiagnosisLocation => qsDiagnosisFeature?.LInventoryLocation ?? string.Empty;

    public QSDiagnosisState QSDiagnosisProgramState => qsDiagnosisFeature is null
        ? QSDiagnosisState.QSDiagnosisStateChecking
        : QSDiagnosisReady ? QSDiagnosisState.QSDiagnosisStateReady : QSDiagnosisState.QSDiagnosisStateMissing;

    public QSDiagnosisState QSDiagnosisStateRead(string[] lFilters)
    {
        if (qsDiagnosisFeature is null)
        {
            return QSDiagnosisState.QSDiagnosisStateChecking;
        }

        return QSDiagnosisReady && QSDiagnosisFiltersCheck(lFilters)
            ? QSDiagnosisState.QSDiagnosisStateReady
            : QSDiagnosisState.QSDiagnosisStateMissing;
    }

    public int QSDiagnosisMissing =>
        qsDiagnosisVideoItems.Concat(qsDiagnosisAudioItems)
            .Count(lItem => QSDiagnosisStateRead(lItem.QSDiagnosisFilters) == QSDiagnosisState.QSDiagnosisStateMissing);

    public QSDiagnosisMood QSDiagnosisMood
    {
        get
        {
            if (qsDiagnosisFeature is null)
            {
                return QSDiagnosisMood.QSDiagnosisMoodChecking;
            }

            if (!QSDiagnosisReady)
            {
                return QSDiagnosisMood.QSDiagnosisMoodAbsent;
            }

            if (QSDiagnosisMissing == 0)
            {
                return QSDiagnosisMood.QSDiagnosisMoodReady;
            }

            bool lVideoAny = qsDiagnosisVideoItems.Any(lItem => QSDiagnosisFiltersCheck(lItem.QSDiagnosisFilters));
            bool lAudioAny = qsDiagnosisAudioItems.Any(lItem => QSDiagnosisFiltersCheck(lItem.QSDiagnosisFilters));
            return lVideoAny && lAudioAny
                ? QSDiagnosisMood.QSDiagnosisMoodWarning
                : QSDiagnosisMood.QSDiagnosisMoodMissing;
        }
    }

    public void QSDiagnosisFeatureSet(LInventoryFeature? lFeature)
    {
        qsDiagnosisFeature = lFeature;
        QSDiagnosisChanged?.Invoke();
    }

    public async Task QSDiagnosisProbeStart()
    {
        int lGeneration = QSDiagnosisGenerationStart();
        QSDiagnosisFeatureSet(null);
        string[] lFilters = QSDiagnosisFiltersRead();
        LInventoryFeature lFeature = await QSDiagnosisFeatureRead(lFilters);
        if (QSDiagnosisGenerationCheck(lGeneration))
        {
            QSDiagnosisFeatureSet(lFeature);
        }
    }

    private int QSDiagnosisGenerationStart() => ++qsDiagnosisGeneration;

    private bool QSDiagnosisGenerationCheck(int lGeneration) => lGeneration == qsDiagnosisGeneration;

    private static string[] QSDiagnosisFiltersRead() =>
        qsDiagnosisVideoItems.Concat(qsDiagnosisAudioItems)
            .SelectMany(lItem => lItem.QSDiagnosisFilters)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static async Task<LInventoryFeature> QSDiagnosisFeatureRead(string[] lFilters)
    {
        try
        {
            return await LInventory.LInventoryFeatureRead(lFilters);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Diagnosis probe failed", lException);
            return new LInventoryFeature(string.Empty, string.Empty, new Dictionary<string, bool>());
        }
    }

    public Task QSDiagnosisReset()
    {
        LInventory.LInventoryReset();
        return QSDiagnosisProbeStart();
    }

    public void QSDiagnosisClose() => qsDiagnosisGeneration++;

    private bool QSDiagnosisFiltersCheck(string[] lFilters) =>
        qsDiagnosisFeature is { } lFeature
        && lFilters.All(lFilter => lFeature.LInventoryMap.TryGetValue(lFilter, out bool lValue) && lValue);
}
