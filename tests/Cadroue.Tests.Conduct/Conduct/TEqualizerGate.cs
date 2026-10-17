using System.Collections.Generic;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TEqualizerGate
{
    private readonly CEqualizer tEqualizerGate = TAtelier.TAtelierCreate()
        .CAtelierInspectorCreate()
        .CInspectorEqualizer;

    private int tEqualizerChanges;
    private int tEqualizerRows;

    public TEqualizerGate()
    {
        tEqualizerGate.CEqualizerChanged += () => tEqualizerChanges++;
        tEqualizerGate.CEqualizerRowsChanged += () => tEqualizerRows++;
    }

    [Fact]
    public void Origin_FlatBands_FlatChosen()
    {
        IReadOnlyList<CChoice> choices = tEqualizerGate.CEqualizerChoices;

        Assert.Equal(10, tEqualizerGate.CEqualizerBands.Count);
        Assert.All(tEqualizerGate.CEqualizerBands, band => Assert.Equal(0, band.CEqualizerGain));
        Assert.Equal("Flat", tEqualizerGate.CEqualizerToken);
        Assert.Equal("Flat", tEqualizerGate.CEqualizerPreset);
        Assert.Equal(0, tEqualizerGate.CEqualizerGainOrigin);
        Assert.Equal("Flat", Assert.Single(choices, row => row.CChoiceChosen).CChoiceToken);
        Assert.Equal("Inspector.Equalizer.Preset.Flat", choices[0].CChoiceKey);
        Assert.Equal(new CChoice(null, "Inspector.Common.Custom", null, false), choices[^1]);
    }

    [Fact]
    public void BandSet_SameBand_IsSilent_AndOutOfRangeIsClamped()
    {
        tEqualizerGate.CEqualizerBandSet(3, 250, 0);
        tEqualizerGate.CEqualizerBandSet(3, 250, 2);
        tEqualizerGate.CEqualizerBandSet(3, 250, 2);
        tEqualizerGate.CEqualizerBandSet(99, 250, 2);

        Assert.Equal(1, tEqualizerChanges);
        Assert.Equal(2, tEqualizerGate.CEqualizerGainRead(3));

        tEqualizerGate.CEqualizerBandSet(0, 5, 99);

        Assert.Equal((20d, 12d), tEqualizerGate.CEqualizerBands[0]);
    }

    [Fact]
    public void BandAddRemove_RowsToldOnCount_TokenKeptWhileDrifted()
    {
        tEqualizerGate.CEqualizerPresetSelect("Bright");
        tEqualizerGate.CEqualizerGainSet(0, tEqualizerGate.CEqualizerGainRead(0));
        Assert.Equal(0, tEqualizerRows);

        tEqualizerGate.CEqualizerBandAdd();
        Assert.Equal(11, tEqualizerGate.CEqualizerBands.Count);
        Assert.Null(tEqualizerGate.CEqualizerPreset);
        Assert.Equal("Bright", tEqualizerGate.CEqualizerToken);
        Assert.Equal(
            new CChoice(null, "Inspector.Common.PresetCustom", "Inspector.Equalizer.Preset.Bright", true),
            tEqualizerGate.CEqualizerChoices[^1]);

        tEqualizerGate.CEqualizerBandRemove(10);
        tEqualizerGate.CEqualizerBandRemove(-1);

        Assert.Equal(2, tEqualizerRows);
        Assert.Equal("Bright", tEqualizerGate.CEqualizerPreset);
    }

    [Fact]
    public void FrequencyCommit_ParsesText_KeepsOnGarbage()
    {
        Assert.Equal(2500, tEqualizerGate.CEqualizerFrequencyCommit(1, "2500"));
        Assert.Equal(1, tEqualizerChanges);

        Assert.Equal(2500, tEqualizerGate.CEqualizerFrequencyCommit(1, "abc"));
        Assert.Equal(1, tEqualizerChanges);
        Assert.Equal(1000, tEqualizerGate.CEqualizerFrequencyCommit(99, "50"));
    }

    [Fact]
    public void ChoiceSelect_Index_PicksPreset_CustomAndUnknownIgnored()
    {
        string token = tEqualizerGate.CEqualizerChoices[1].CChoiceToken!;

        tEqualizerGate.CEqualizerChoiceSelect(1);
        Assert.Equal(token, tEqualizerGate.CEqualizerToken);
        Assert.True(tEqualizerGate.CEqualizerChoices[1].CChoiceChosen);
        int changes = tEqualizerChanges;

        tEqualizerGate.CEqualizerChoiceSelect(1);
        tEqualizerGate.CEqualizerChoiceSelect(tEqualizerGate.CEqualizerChoices.Count - 1);
        tEqualizerGate.CEqualizerPresetSelect("Unknown");

        Assert.Equal(changes, tEqualizerChanges);
        Assert.Equal(token, tEqualizerGate.CEqualizerToken);
        Assert.Single(tEqualizerGate.CEqualizerChoices, row => row.CChoiceChosen);
    }

    [Fact]
    public void SwitchAndPersistent_TellOncePerChange()
    {
        tEqualizerGate.CEqualizerActiveSet(true);
        tEqualizerGate.CEqualizerActiveSet(true);
        tEqualizerGate.CEqualizerPersistentSet(true);
        tEqualizerGate.CEqualizerPersistentSet(true);

        Assert.True(tEqualizerGate.CEqualizerActive);
        Assert.True(tEqualizerGate.CEqualizerPersistent);
        Assert.Equal(2, tEqualizerChanges);
    }
}
