using System.Globalization;
using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record LEqualizerBand(int LEqualizerBandIndex, string LEqualizerBandFrequency, double LEqualizerBandGain);

public sealed class LEqualizer
{
    private const string LEqualizerFrequencyPattern = "0.###";

    private bool lEqualizerActive;
    private List<LWorkBand> lEqualizerBands = LWorkEqualizerStep.LWorkBandsCreate().ToList();
    private string? lEqualizerToken;
    private bool lEqualizerPersistent;

    public LEqualizer()
    {
        lEqualizerToken = LEqualizerMatchRead();
    }

    public event Action? LEqualizerChange;
    public event Action? LEqualizerRowsChange;

    public bool LEqualizerActive => lEqualizerActive;

    public IReadOnlyList<LWorkBand> LEqualizerBands => lEqualizerBands;

    public string? LEqualizerToken => lEqualizerToken;

    public bool LEqualizerPersistent => lEqualizerPersistent;

    public double LEqualizerGainLeast => LContourCatalog.LContourGainLeast;

    public double LEqualizerGainMost => LContourCatalog.LContourGainMost;

    public string LEqualizerRemoveTip => LLocalization.LLocalizationTextRead("Inspector.Equalizer.Remove");

    public IReadOnlyList<LEqualizerBand> LEqualizerRows => lEqualizerBands
        .Select((lBand, lIndex) => new LEqualizerBand(
            lIndex,
            lBand.LWorkBandFrequency.ToString(LEqualizerFrequencyPattern, CultureInfo.InvariantCulture),
            lBand.LWorkBandGain))
        .ToList();

    public LInspectorChoice LEqualizerChoiceRead() => LInspectorPlan.LInspectorChoiceRead(
        LContourCatalog.LContourTokensRead(), LEqualizerKeyRead, lEqualizerToken, LEqualizerMatchRead());

    public static string LEqualizerKeyRead(string lToken) => lToken switch
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

    public double LEqualizerGainRead(int lIndex) =>
        lIndex >= 0 && lIndex < lEqualizerBands.Count ? lEqualizerBands[lIndex].LWorkBandGain : 0;

    public string LEqualizerFrequencyRead(int lIndex) =>
        LEqualizerFrequencyResolve(lIndex).ToString(LEqualizerFrequencyPattern, CultureInfo.InvariantCulture);

    public string LEqualizerFrequencyCommit(int lIndex, string lText)
    {
        double lCurrent = LEqualizerFrequencyResolve(lIndex);
        double lFrequency = LInspector.LInspectorValueCommit(lText, lCurrent, null, null);
        LEqualizerBandSet(lIndex, lFrequency, LEqualizerGainRead(lIndex));
        return LInspector.LInspectorValueFormat(lText, LEqualizerFrequencyResolve(lIndex), LEqualizerFrequencyPattern);
    }

    public void LEqualizerGainSet(int lIndex, double lGain) =>
        LEqualizerBandSet(lIndex, LEqualizerFrequencyResolve(lIndex), lGain);

    public void LEqualizerChoiceSelect(int lIndex)
    {
        string? lToken = LInspectorPlan.LInspectorChoiceResolve(
            LContourCatalog.LContourTokensRead(), lIndex, lEqualizerToken, LEqualizerMatchRead());
        if (lToken is not null)
        {
            LEqualizerPresetSelect(lToken);
        }
    }

    public LWorkAudioStep LEqualizerStepRead() =>
        LWorkAudioStep.LWorkEqualizerCreate(lEqualizerActive, lEqualizerBands.ToArray());

    public string? LEqualizerMatchRead() => LContourCatalog.LContourPresetFind(
        lEqualizerBands.Select(lBand => lBand.LWorkBandFrequency).ToArray(),
        lEqualizerBands.Select(lBand => lBand.LWorkBandGain).ToArray());

    public void LEqualizerStepSet(LWorkAudioStep lStep)
    {
        IReadOnlyList<LWorkBand> lBands = lStep is LWorkEqualizerStep lEqualizer
            ? lEqualizer.LWorkEqualizerBands
            : LWorkEqualizerStep.LWorkBandsCreate();
        LEqualizerBandsApply(lStep.LWorkStepActive, lBands.ToList(), null);
    }

    public void LEqualizerActiveSet(bool lActive)
    {
        if (lEqualizerActive == lActive)
        {
            return;
        }

        lEqualizerActive = lActive;
        LEqualizerChange?.Invoke();
    }

    public void LEqualizerBandSet(int lIndex, double lFrequency, double lGain)
    {
        if (lIndex < 0 || lIndex >= lEqualizerBands.Count)
        {
            return;
        }

        LWorkBand lBand = LEqualizerNormalize(lFrequency, lGain);
        if (lEqualizerBands[lIndex] == lBand)
        {
            return;
        }

        List<LWorkBand> lBands = lEqualizerBands.ToList();
        lBands[lIndex] = lBand;
        LEqualizerBandsApply(lEqualizerActive, lBands, lEqualizerToken);
    }

    public void LEqualizerBandAdd()
    {
        List<LWorkBand> lBands = lEqualizerBands.ToList();
        lBands.Add(new LWorkBand(LContourCatalog.LContourFrequencyDefault, 0));
        LEqualizerBandsApply(lEqualizerActive, lBands, lEqualizerToken);
    }

    public void LEqualizerBandRemove(int lIndex)
    {
        if (lIndex < 0 || lIndex >= lEqualizerBands.Count)
        {
            return;
        }

        List<LWorkBand> lBands = lEqualizerBands.ToList();
        lBands.RemoveAt(lIndex);
        LEqualizerBandsApply(lEqualizerActive, lBands, lEqualizerToken);
    }

    public void LEqualizerPresetSelect(string lToken)
    {
        if (LContourCatalog.LContourGainsRead(lToken) is not { } lGains)
        {
            return;
        }

        double[] lGrid = LContourCatalog.LContourBandGrid;
        var lBands = new List<LWorkBand>();
        for (int lIndex = 0; lIndex < lGrid.Length; lIndex++)
        {
            lBands.Add(new LWorkBand(lGrid[lIndex], lGains[lIndex]));
        }

        LEqualizerBandsApply(lEqualizerActive, lBands, lToken);
    }

    public void LEqualizerPersistentSet(bool lPersistent)
    {
        if (lEqualizerPersistent == lPersistent)
        {
            return;
        }

        lEqualizerPersistent = lPersistent;
        LEqualizerChange?.Invoke();
    }

    private double LEqualizerFrequencyResolve(int lIndex) =>
        lIndex >= 0 && lIndex < lEqualizerBands.Count
            ? lEqualizerBands[lIndex].LWorkBandFrequency
            : LContourCatalog.LContourFrequencyDefault;

    private static LWorkBand LEqualizerNormalize(double lFrequency, double lGain) =>
        ((LWorkEqualizerStep)LWorkAudioStep.LWorkEqualizerCreate(false, new[] { new LWorkBand(lFrequency, lGain) }))
            .LWorkEqualizerBands[0];

    private void LEqualizerBandsApply(bool lActive, List<LWorkBand> lBands, string? lFallback)
    {
        string? lToken = LContourCatalog.LContourPresetFind(
            lBands.Select(lBand => lBand.LWorkBandFrequency).ToArray(),
            lBands.Select(lBand => lBand.LWorkBandGain).ToArray()) ?? lFallback;
        if (lEqualizerActive == lActive && lEqualizerBands.SequenceEqual(lBands) && lEqualizerToken == lToken)
        {
            return;
        }

        bool lRows = lEqualizerBands.Count != lBands.Count;
        lEqualizerActive = lActive;
        lEqualizerBands = lBands;
        lEqualizerToken = lToken;
        if (lRows)
        {
            LEqualizerRowsChange?.Invoke();
        }

        LEqualizerChange?.Invoke();
    }
}
