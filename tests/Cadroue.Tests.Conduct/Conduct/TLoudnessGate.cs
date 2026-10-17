using System.Linq;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TLoudnessGate
{
    private readonly TLoudnessPort tLoudnessPort = new();
    private readonly TAudioPort tLoudnessAudio = new();
    private readonly CInspector tLoudnessInspector;
    private readonly CLoudness tLoudnessGate;
    private int tLoudnessChanges;

    public TLoudnessGate()
    {
        tLoudnessInspector = TAtelier.TAtelierCreate(port => port with
            {
                LAtelierLoudness = tLoudnessPort,
                LAtelierAudio = tLoudnessAudio
            })
            .CAtelierInspectorCreate();
        tLoudnessGate = tLoudnessInspector.CInspectorLoudness;
        tLoudnessGate.CLoudnessChanged += () => tLoudnessChanges++;
    }

    [Fact]
    public void Origin_DefaultLoudness_AudiobookChosen()
    {
        Assert.False(tLoudnessGate.CLoudnessActive);
        Assert.False(tLoudnessGate.CLoudnessDynamic);
        Assert.True(tLoudnessGate.CLoudnessTwoPass);
        Assert.Equal([-21d, -2d, 6d, 300d, 21d, 10d, 6d], tLoudnessGate.CLoudnessValues);
        Assert.Equal("Audiobook", tLoudnessGate.CLoudnessToken);
        Assert.Equal(0, tLoudnessGate.CLoudnessModes.ToList().FindIndex(row => row.CChoiceChosen));
        Assert.Equal(4, tLoudnessGate.CLoudnessChoicesRead(false).ToList().FindIndex(row => row.CChoiceChosen));
    }

    [Fact]
    public void ModeSelect_Dynamic_MatchBecomesToken_OutOfRangeIgnored()
    {
        tLoudnessGate.CLoudnessModeSelect(2);
        Assert.Equal(0, tLoudnessChanges);

        tLoudnessGate.CLoudnessModeSelect(1);

        Assert.True(tLoudnessGate.CLoudnessDynamic);
        Assert.Equal("Leveler", tLoudnessGate.CLoudnessToken);
        Assert.Equal(1, tLoudnessGate.CLoudnessModes.ToList().FindIndex(row => row.CChoiceChosen));
        Assert.Equal(1, tLoudnessChanges);
    }

    [Fact]
    public void PresetSelect_Twice_ToldOnce_ValueDrift_KeepsToken()
    {
        tLoudnessGate.CLoudnessPresetSelect("Streaming");
        tLoudnessGate.CLoudnessPresetSelect("Streaming");

        Assert.Equal(1, tLoudnessChanges);
        Assert.Equal(-14, tLoudnessGate.CLoudnessValueRead(false, 0));
        Assert.Equal(9, tLoudnessGate.CLoudnessValueRead(false, 2));

        tLoudnessGate.CLoudnessValueSet(false, 0, -16);

        Assert.Null(tLoudnessGate.CLoudnessPreset);
        Assert.Equal("Streaming", tLoudnessGate.CLoudnessToken);
        CChoice custom = tLoudnessGate.CLoudnessChoicesRead(false)[^1];
        Assert.True(custom.CChoiceChosen);
        Assert.Equal("Inspector.Common.PresetCustom", custom.CChoiceKey);
        Assert.Equal("Inspector.Normalize.Streaming", custom.CChoiceBasis);
    }

    [Fact]
    public void ValueSet_HeldInsideRange_RowOutOfRangeChangesNothing()
    {
        tLoudnessGate.CLoudnessValueSet(false, 1, 5);
        tLoudnessGate.CLoudnessValueSet(true, 7, 5);

        Assert.Equal(0, tLoudnessGate.CLoudnessValueRead(false, 1));
        Assert.Equal(1, tLoudnessChanges);
        Assert.Equal(300, tLoudnessGate.CLoudnessValueRead(true, 9));
    }

    [Fact]
    public void ChoiceSelect_OnlyHeldMode_CustomIgnored()
    {
        tLoudnessGate.CLoudnessChoiceSelect(true, 0);
        tLoudnessGate.CLoudnessChoiceSelect(false, 8);
        Assert.Equal(0, tLoudnessChanges);

        tLoudnessGate.CLoudnessChoiceSelect(false, 2);

        Assert.Equal("Podcast", tLoudnessGate.CLoudnessToken);
        Assert.Equal(2, tLoudnessGate.CLoudnessChoicesRead(false).ToList().FindIndex(row => row.CChoiceChosen));
    }

    [Fact]
    public void DefaultRead_FollowsTokenOfMode()
    {
        tLoudnessGate.CLoudnessPresetSelect("Loud");

        Assert.Equal(-9, tLoudnessGate.CLoudnessDefaultRead(false, 0));
        Assert.Equal(300, tLoudnessGate.CLoudnessDefaultRead(true, 0));
    }

    [Fact]
    public void InspectorAudioAccept_PlanLeveling_SetsStepAndPreset()
    {
        TInspectorPort plan = new() { TInspectorLeveling = (true, true, [-21, -2, 6, 200, 15, 12, 8], false, "Voice") };

        tLoudnessInspector.CInspectorAudioAccept(plan);

        Assert.Contains("leveling", plan.TInspectorCalls);
        Assert.True(tLoudnessGate.CLoudnessActive);
        Assert.True(tLoudnessGate.CLoudnessDynamic);
        Assert.False(tLoudnessGate.CLoudnessTwoPass);
        Assert.Equal("Voice", tLoudnessGate.CLoudnessToken);
    }

    [Fact]
    public void InspectorPersistentAccept_PlanCarriesLeveling_KeepsIt()
    {
        tLoudnessInspector.CInspectorAudioAccept(
            new TInspectorPort { TInspectorPresent = (false, false, false, false, false, true) });

        tLoudnessInspector.CInspectorPersistentAccept(false);

        Assert.True(tLoudnessGate.CLoudnessPersistent);
    }

    [Fact]
    public void InspectorPersistentRead_KeptLeveling_HandsHeldValues()
    {
        tLoudnessGate.CLoudnessActiveSet(true);
        tLoudnessGate.CLoudnessPersistentSet(true);

        tLoudnessInspector.CInspectorPersistentRead();

        var leveling = tLoudnessAudio.TAudioTools.Item5;
        Assert.NotNull(leveling);
        Assert.True(leveling.Value.Item1);
        Assert.False(leveling.Value.Item2);
        Assert.Equal(tLoudnessGate.CLoudnessValues, leveling.Value.Item3);
        Assert.True(leveling.Value.Item4);
    }
}
