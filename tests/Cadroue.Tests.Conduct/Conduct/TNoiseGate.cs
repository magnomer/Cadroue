using System.Collections.Generic;
using System.Linq;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TNoiseGate
{
    private readonly TNoisePort tNoisePort = new();
    private readonly CInspector tNoiseInspector;
    private readonly CNoise tNoiseGate;
    private int tNoiseChanges;

    public TNoiseGate()
    {
        tNoiseInspector = TAtelier.TAtelierCreate(port => port with { LAtelierNoise = tNoisePort })
            .CAtelierInspectorCreate();
        tNoiseGate = tNoiseInspector.CInspectorNoise;
        tNoiseGate.CNoiseChanged += () => tNoiseChanges++;
    }

    [Fact]
    public void Origin_MediumValues_MediumChosen()
    {
        Assert.Equal(new double[] { 12, 6, -50, -38, 0.5 }, tNoiseGate.CNoiseValues);
        Assert.False(tNoiseGate.CNoiseActive);
        Assert.Equal("Medium", tNoiseGate.CNoiseToken);
        Assert.Equal("Medium", tNoiseGate.CNoisePreset);
        Assert.Equal(1, tNoiseGate.CNoiseChoices.ToList().FindIndex(row => row.CChoiceChosen));
        Assert.True(tNoiseGate.CNoiseTypes[0].CChoiceChosen);
    }

    [Fact]
    public void PresetSelect_TakesValuesAndType_OneEngineCall()
    {
        tNoiseGate.CNoisePresetSelect("Shellac");

        Assert.Equal(["preset"], tNoisePort.TNoiseCalls);
        Assert.Equal("Shellac", tNoiseGate.CNoiseToken);
        Assert.Equal(2, tNoiseGate.CNoiseType);
        Assert.Equal(8, tNoiseGate.CNoiseValueRead(1));
        Assert.Equal(1, tNoiseChanges);
    }

    [Fact]
    public void PresetSelect_Unknown_Ignored()
    {
        tNoiseGate.CNoisePresetSelect("Nowhere");

        Assert.Equal("Medium", tNoiseGate.CNoiseToken);
        Assert.Equal(0, tNoiseChanges);
    }

    [Fact]
    public void ValueSet_Clamps_KeepsToken_CustomShowsBasis()
    {
        tNoiseGate.CNoiseValueSet(0, 99);

        Assert.Equal(30, tNoiseGate.CNoiseValueRead(0));
        Assert.Equal("Medium", tNoiseGate.CNoiseToken);
        Assert.Null(tNoiseGate.CNoisePreset);
        CChoice custom = tNoiseGate.CNoiseChoices[^1];
        Assert.True(custom.CChoiceChosen);
        Assert.Equal("Inspector.Common.PresetCustom", custom.CChoiceKey);
        Assert.Equal("Inspector.Noise.Medium", custom.CChoiceBasis);
        Assert.Equal(12, tNoiseGate.CNoiseDefaultRead(0));
    }

    [Fact]
    public void ValueSet_SlotOutOfRange_NotTold()
    {
        tNoiseGate.CNoiseValueSet(7, 1);

        Assert.Equal(0, tNoiseChanges);
        Assert.Equal(12, tNoiseGate.CNoiseValueRead(7));
    }

    [Fact]
    public void ChoiceSelect_CustomAndChosen_Ignored()
    {
        tNoiseGate.CNoiseChoiceSelect(1);
        tNoiseGate.CNoiseChoiceSelect(tNoiseGate.CNoiseChoices.Count - 1);
        tNoiseGate.CNoiseChoiceSelect(-1);

        Assert.Empty(tNoisePort.TNoiseCalls);
        Assert.Equal(0, tNoiseChanges);
    }

    [Fact]
    public void TypeSelect_MatchFollowsType_OutOfRangeIgnored()
    {
        tNoiseGate.CNoiseTypeSelect(1);
        tNoiseGate.CNoiseTypeSelect(3);

        Assert.Equal(1, tNoiseGate.CNoiseType);
        Assert.Equal(1, tNoiseChanges);
        Assert.Equal("Vinyl", tNoiseGate.CNoisePreset);
        Assert.Equal("Medium", tNoiseGate.CNoiseToken);
    }

    [Fact]
    public void SwitchesAndPersistence_ToldOnChangeOnly()
    {
        tNoiseGate.CNoiseActiveSet(true);
        tNoiseGate.CNoiseActiveSet(true);
        tNoiseGate.CNoiseTrackSet(true);
        tNoiseGate.CNoisePersistentSet(true);
        tNoiseGate.CNoisePersistentSet(true);

        Assert.True(tNoiseGate.CNoiseActive);
        Assert.True(tNoiseGate.CNoiseTrack);
        Assert.True(tNoiseGate.CNoisePersistent);
        Assert.Equal(3, tNoiseChanges);
        Assert.Equal("Medium", tNoiseGate.CNoiseToken);
    }

    [Fact]
    public void AudioAccept_TakesPlanMatchAsToken()
    {
        var plan = new TInspectorPort
        {
            TInspectorNoise = (true, new List<double> { 10, 5, -50, -40, 0.8 }, false, 0, "Dialogue")
        };

        tNoiseInspector.CInspectorAudio.CInspectorAudioAccept(plan);

        Assert.True(tNoiseGate.CNoiseActive);
        Assert.Equal("Dialogue", tNoiseGate.CNoiseToken);
        Assert.Contains("noise", plan.TInspectorCalls);
        Assert.Equal(1, tNoiseChanges);
    }
}
