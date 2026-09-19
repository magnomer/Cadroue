using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public enum LSDiagnosisState
{
    LSDiagnosisStateChecking,
    LSDiagnosisStateReady,
    LSDiagnosisStateMissing
}

public enum LSDiagnosisMood
{
    LSDiagnosisMoodChecking,
    LSDiagnosisMoodReady,
    LSDiagnosisMoodWarning,
    LSDiagnosisMoodMissing,
    LSDiagnosisMoodAbsent
}

public sealed record LSDiagnosisItem(string LSDiagnosisLabel, string[] LSDiagnosisFilters);

public sealed class LSDiagnosis
{
    private static readonly LSDiagnosisItem[] lsDiagnosisVideoItems =
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

    private static readonly LSDiagnosisItem[] lsDiagnosisAudioItems =
    [
        new("Diagnosis.Feature.Volume", ["volume"]),
        new("Diagnosis.Feature.Loudness", ["loudnorm"]),
        new("Diagnosis.Feature.Dynamic", ["dynaudnorm"]),
        new("Diagnosis.Feature.Equalizer", ["equalizer"]),
        new("Diagnosis.Feature.Highpass", ["highpass"]),
        new("Diagnosis.Feature.Lowpass", ["lowpass"]),
        new("Diagnosis.Feature.Noise", ["afftdn"])
    ];

    private int lsDiagnosisGeneration;
    private LInventoryFeature? lsDiagnosisFeature;

    public event Action? LSDiagnosisChange;

    public static IReadOnlyList<LSDiagnosisItem> LSDiagnosisVideoItems => lsDiagnosisVideoItems;

    public static IReadOnlyList<LSDiagnosisItem> LSDiagnosisAudioItems => lsDiagnosisAudioItems;

    public string LSDiagnosisTitle => LLocalization.LLocalizationTextRead("Diagnosis.Window.Title");

    public bool LSDiagnosisChecking => lsDiagnosisFeature is null;

    public bool LSDiagnosisReady => !string.IsNullOrWhiteSpace(lsDiagnosisFeature?.LInventoryVersion);

    public string LSDiagnosisVersion => lsDiagnosisFeature?.LInventoryVersion ?? string.Empty;

    public string LSDiagnosisLocation => lsDiagnosisFeature?.LInventoryLocation ?? string.Empty;

    public LSDiagnosisState LSDiagnosisProgramState => lsDiagnosisFeature is null
        ? LSDiagnosisState.LSDiagnosisStateChecking
        : LSDiagnosisReady ? LSDiagnosisState.LSDiagnosisStateReady : LSDiagnosisState.LSDiagnosisStateMissing;

    public LSDiagnosisState LSDiagnosisStateRead(string[] lFilters)
    {
        if (lsDiagnosisFeature is null)
        {
            return LSDiagnosisState.LSDiagnosisStateChecking;
        }

        return LSDiagnosisReady && LSDiagnosisFiltersCheck(lFilters)
            ? LSDiagnosisState.LSDiagnosisStateReady
            : LSDiagnosisState.LSDiagnosisStateMissing;
    }

    public int LSDiagnosisMissing =>
        lsDiagnosisVideoItems.Concat(lsDiagnosisAudioItems)
            .Count(lItem => LSDiagnosisStateRead(lItem.LSDiagnosisFilters) == LSDiagnosisState.LSDiagnosisStateMissing);

    public LSDiagnosisMood LSDiagnosisMood
    {
        get
        {
            if (lsDiagnosisFeature is null)
            {
                return LSDiagnosisMood.LSDiagnosisMoodChecking;
            }

            if (!LSDiagnosisReady)
            {
                return LSDiagnosisMood.LSDiagnosisMoodAbsent;
            }

            if (LSDiagnosisMissing == 0)
            {
                return LSDiagnosisMood.LSDiagnosisMoodReady;
            }

            bool lVideoAny = lsDiagnosisVideoItems.Any(lItem => LSDiagnosisFiltersCheck(lItem.LSDiagnosisFilters));
            bool lAudioAny = lsDiagnosisAudioItems.Any(lItem => LSDiagnosisFiltersCheck(lItem.LSDiagnosisFilters));
            return lVideoAny && lAudioAny
                ? LSDiagnosisMood.LSDiagnosisMoodWarning
                : LSDiagnosisMood.LSDiagnosisMoodMissing;
        }
    }

    public void LSDiagnosisFeatureSet(LInventoryFeature? lFeature)
    {
        lsDiagnosisFeature = lFeature;
        LSDiagnosisChange?.Invoke();
    }

    public async Task LSDiagnosisProbeStart()
    {
        int lGeneration = ++lsDiagnosisGeneration;
        LSDiagnosisFeatureSet(null);
        string[] lFilters = lsDiagnosisVideoItems.Concat(lsDiagnosisAudioItems)
            .SelectMany(lItem => lItem.LSDiagnosisFilters)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        LInventoryFeature lFeature;
        try
        {
            lFeature = await LInventory.LInventoryFeatureRead(lFilters);
        }
        catch (Exception lException)
        {
            LTraceLog.LTraceErrorRecord("Diagnosis probe failed", lException);
            lFeature = new LInventoryFeature(string.Empty, string.Empty, new Dictionary<string, bool>());
        }

        if (lGeneration == lsDiagnosisGeneration)
        {
            LSDiagnosisFeatureSet(lFeature);
        }
    }

    public Task LSDiagnosisReset()
    {
        LInventory.LInventoryReset();
        return LSDiagnosisProbeStart();
    }

    public void LSDiagnosisClose() => lsDiagnosisGeneration++;

    private bool LSDiagnosisFiltersCheck(string[] lFilters) =>
        lsDiagnosisFeature is { } lFeature
        && lFilters.All(lFilter => lFeature.LInventoryMap.TryGetValue(lFilter, out bool lValue) && lValue);
}
