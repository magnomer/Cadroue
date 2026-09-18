using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed class LEqualizer
{
    private bool lEqualizerActive;
    private List<LWorkBand> lEqualizerBands = LWorkEqualizerStep.LWorkBandsCreate().ToList();
    private string? lEqualizerToken;
    private bool lEqualizerPersistent;

    public LEqualizer()
    {
        lEqualizerToken = LEqualizerMatchRead();
    }

    public event Action? LEqualizerChange;

    public bool LEqualizerActive => lEqualizerActive;

    public IReadOnlyList<LWorkBand> LEqualizerBands => lEqualizerBands;

    public string? LEqualizerToken => lEqualizerToken;

    public bool LEqualizerPersistent => lEqualizerPersistent;

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

        lEqualizerActive = lActive;
        lEqualizerBands = lBands;
        lEqualizerToken = lToken;
        LEqualizerChange?.Invoke();
    }
}
