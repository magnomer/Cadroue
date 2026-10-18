using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TAudioGate
{
    private readonly TAudioPort tAudioPort = new();
    private readonly CInspector tAudioGate;

    public TAudioGate()
    {
        tAudioGate = TAtelier.TAtelierCreate(port => port with { LAtelierAudio = tAudioPort })
            .CAtelierInspectorCreate();
    }

    [Fact]
    public void InspectorPersistentRead_NothingKept_AsksOnceWithNoTool()
    {
        var plan = tAudioGate.CInspectorAudio.CInspectorPersistentRead();

        Assert.Same(tAudioPort.TAudioPlan, plan);
        Assert.Equal(["persistent"], tAudioPort.TAudioCalls);
        Assert.Equal((null, null, null, null, null, null), tAudioPort.TAudioTools);
        Assert.False(tAudioPort.TAudioSkip);
    }

    [Fact]
    public void InspectorPersistentRead_KeptTools_HandsTheirHeldValues()
    {
        tAudioGate.CInspectorHighpass.CFilterPersistentSet(true);
        tAudioGate.CInspectorVolume.CVolumeStepSet(true, -6);
        tAudioGate.CInspectorVolume.CVolumePersistentSet(true);
        tAudioGate.CInspectorEqualizer.CEqualizerActiveSet(true);
        tAudioGate.CInspectorEqualizer.CEqualizerPersistentSet(true);

        tAudioGate.CInspectorAudio.CInspectorPersistentRead();

        var (highpass, lowpass, noise, volume, leveling, equalizer) = tAudioPort.TAudioTools;
        Assert.NotNull(highpass);
        Assert.Equal(tAudioGate.CInspectorHighpass.CFilterActive, highpass.Value.Item1);
        Assert.Equal(tAudioGate.CInspectorHighpass.CFilterValues, highpass.Value.Item2);
        Assert.Equal(tAudioGate.CInspectorHighpass.CFilterPoles, highpass.Value.Item3);
        Assert.Null(lowpass);
        Assert.Null(noise);
        Assert.Equal((true, -6d), volume);
        Assert.Null(leveling);
        Assert.NotNull(equalizer);
        Assert.True(equalizer.Value.Item1);
        Assert.Equal(tAudioGate.CInspectorEqualizer.CEqualizerBands, equalizer.Value.Item2);
    }

    [Fact]
    public void InspectorPersistentAccept_Plan_KeepsEachToolItCarries()
    {
        TInspectorPort plan = new() { TInspectorPresent = (true, false, true, false, true, false) };
        tAudioGate.CInspectorAudio.CInspectorAudioAccept(plan);
        plan.TInspectorCalls.Clear();

        tAudioGate.CInspectorAudio.CInspectorPersistentAccept(false);

        Assert.Equal(["present"], plan.TInspectorCalls);
        Assert.True(tAudioGate.CInspectorHighpass.CFilterPersistent);
        Assert.False(tAudioGate.CInspectorLowpass.CFilterPersistent);
        Assert.True(tAudioGate.CInspectorNoise.CNoisePersistent);
        Assert.False(tAudioGate.CInspectorVolume.CVolumePersistent);
        Assert.True(tAudioGate.CInspectorEqualizer.CEqualizerPersistent);
    }

    [Fact]
    public void InspectorPersistentAccept_ToolLacking_StaysKept()
    {
        tAudioGate.CInspectorLowpass.CFilterPersistentSet(true);

        tAudioGate.CInspectorAudio.CInspectorAudioAccept(new TInspectorPort());

        tAudioGate.CInspectorAudio.CInspectorPersistentAccept(false);

        Assert.True(tAudioGate.CInspectorLowpass.CFilterPersistent);
    }

    [Fact]
    public void InspectorPersistentAccept_NoPlanYet_KeepsNothing()
    {
        tAudioGate.CInspectorAudio.CInspectorPersistentAccept(false);

        Assert.False(tAudioGate.CInspectorHighpass.CFilterPersistent);
        Assert.False(tAudioGate.CInspectorVolume.CVolumePersistent);
    }
}
