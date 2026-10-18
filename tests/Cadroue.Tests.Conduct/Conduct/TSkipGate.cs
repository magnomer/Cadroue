using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TSkipGate
{
    private readonly TAudioPort tSkipAudio = new();
    private readonly CInspector tSkipInspector;
    private readonly CSkip tSkipGate;
    private int tSkipChanges;
    private int tSkipSwitches;

    public TSkipGate()
    {
        tSkipInspector = TAtelier.TAtelierCreate(port => port with { LAtelierAudio = tSkipAudio })
            .CAtelierInspectorCreate();
        tSkipGate = tSkipInspector.CInspectorSkip;
        tSkipGate.CSkipChanged += () => tSkipChanges++;
        tSkipGate.CSkipActiveChanged += () => tSkipSwitches++;
    }

    [Fact]
    public void SkipActiveSet_Change_ToldAsChangeAndSwitch_RepeatIsSilent()
    {
        tSkipGate.CSkipActiveSet(true);
        tSkipGate.CSkipActiveSet(true);

        Assert.True(tSkipGate.CSkipActive);
        Assert.Equal(1, tSkipChanges);
        Assert.Equal(1, tSkipSwitches);
    }

    [Fact]
    public void SkipPersistentSet_Change_ToldAsChangeOnly()
    {
        tSkipGate.CSkipPersistentSet(true);
        tSkipGate.CSkipPersistentSet(true);

        Assert.True(tSkipGate.CSkipPersistent);
        Assert.Equal(1, tSkipChanges);
        Assert.Equal(0, tSkipSwitches);
    }

    [Fact]
    public void InspectorPersistentAccept_NoPlanYet_StillKeepsSkipAsTold()
    {
        tSkipInspector.CInspectorAudio.CInspectorPersistentAccept(true);

        Assert.True(tSkipGate.CSkipPersistent);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    public void InspectorPersistentRead_Skip_OnlyWhenKeptAndOn(bool persistent, bool active, bool expected)
    {
        tSkipGate.CSkipPersistentSet(persistent);
        tSkipGate.CSkipActiveSet(active);

        tSkipInspector.CInspectorAudio.CInspectorPersistentRead();

        Assert.Equal(expected, tSkipAudio.TAudioSkip);
    }
}
