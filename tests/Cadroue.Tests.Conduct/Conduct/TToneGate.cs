using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TToneGate
{
    private readonly TTonePort tTonePort = new();
    private readonly CInspector tToneInspector;
    private readonly CTone tToneGate;
    private int tToneNotices;

    public TToneGate()
    {
        tToneInspector = TAtelier.TAtelierCreate(port => port with { LAtelierTone = tTonePort })
            .CAtelierInspectorCreate();
        tToneGate = tToneInspector.CInspectorTone;
        tToneGate.CToneChanged += () => tToneNotices++;
    }

    [Fact]
    public void ToneCreate_Fresh_HoldsOriginsAndRanges()
    {
        Assert.Equal([false, false, false], tToneGate.CToneActives);
        Assert.Equal([0d, 100d, 100d], tToneGate.CToneValues);
        Assert.Equal([false, false, false], tToneGate.CTonePersistents);
        Assert.Equal([0d, 100d, 100d], tToneGate.CToneOrigins);
        Assert.Equal([-100d, 0d, 0d], tToneGate.CToneLeasts);
        Assert.Equal([100d, 200d, 200d], tToneGate.CToneMosts);
        Assert.True(tToneGate.CToneCapable);
    }

    [Fact]
    public void ToneValueSet_Brightness_HeldAsGiven_RepeatIsSilent()
    {
        tToneGate.CToneBrightnessSet(250);
        tToneGate.CToneBrightnessSet(250);

        Assert.Equal(250, tToneGate.CToneValues[0]);
        Assert.Equal(1, tToneNotices);
    }

    [Fact]
    public void ToneValueSet_ContrastAndSaturation_ClampToRange()
    {
        tToneGate.CToneContrastSet(250);
        tToneGate.CToneContrastSet(200);
        tToneGate.CToneSaturationSet(-5);

        Assert.Equal(200, tToneGate.CToneValues[1]);
        Assert.Equal(0, tToneGate.CToneValues[2]);
        Assert.Equal(2, tToneNotices);
    }

    [Fact]
    public void ToneActiveSet_OnlyThatSlot_KeepsValue()
    {
        tToneGate.CToneValueSet(2, 140);
        tToneGate.CToneActiveSet(2, true);
        tToneGate.CToneActiveSet(2, true);

        Assert.Equal([false, false, true], tToneGate.CToneActives);
        Assert.Equal(140, tToneGate.CToneValues[2]);
        Assert.Equal(2, tToneNotices);
    }

    [Fact]
    public void TonePersistentSet_RaisesOnlyOnChange()
    {
        tToneGate.CTonePersistentSet(1, true);
        tToneGate.CTonePersistentSet(1, true);

        Assert.Equal([false, true, false], tToneGate.CTonePersistents);
        Assert.Equal(1, tToneNotices);
    }

    [Fact]
    public void ToneCapableSet_RepeatedValue_IsSilent()
    {
        tToneGate.CToneCapableSet(false);
        tToneGate.CToneCapableSet(false);

        Assert.False(tToneGate.CToneCapable);
        Assert.Equal(1, tToneNotices);
    }

    [Fact]
    public void ToneStepSet_Plan_NormalizesEverySlot_TellsOnce()
    {
        tToneGate.CToneStepSet([true, false, true], [-300, 260, 50]);
        tToneGate.CToneStepSet([true, false, true], [-300, 200, 50]);

        Assert.Equal([true, false, true], tToneGate.CToneActives);
        Assert.Equal([-300d, 200d, 50d], tToneGate.CToneValues);
        Assert.Equal(1, tToneNotices);
    }

    [Fact]
    public void ToneStepSet_NoValues_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tToneGate.CToneStepSet(null!, [0, 100, 100]));
        Assert.Throws<ArgumentNullException>(() => tToneGate.CToneStepSet([false, false, false], null!));
    }

    [Fact]
    public void InspectorVideoAccept_Plan_SetsTone()
    {
        TInspectorPort plan = new() { TInspectorTone = ([true, true, false], [12, 150, 80]) };

        tToneInspector.CInspectorVideo.CInspectorVideoAccept(plan);

        Assert.Equal([true, true, false], tToneGate.CToneActives);
        Assert.Equal([12d, 150d, 80d], tToneGate.CToneValues);
    }

    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void ToneNoticeShown_NeitherFlyleafNorMpv_Shows(bool flyleaf, bool mpv, bool shown)
    {
        tTonePort.TToneFlyleaf = flyleaf;
        tTonePort.TToneMpv = mpv;

        Assert.Equal(shown, tToneGate.CToneNoticeShown);
        Assert.Equal(["preview"], tTonePort.TToneCalls);
    }

    [Fact]
    public void AtelierCreate_NoTonePort_Throws() =>
        Assert.Throws<ArgumentNullException>(() => TAtelier.TAtelierCreate(port => port with { LAtelierTone = null! }));
}
