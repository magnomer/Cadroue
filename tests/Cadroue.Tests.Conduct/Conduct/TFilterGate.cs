using System.Collections.Generic;
using System.Linq;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TFilterGate
{
    private readonly TFilterPort tFilterPort = new();
    private readonly CInspector tFilterInspector;
    private readonly CFilter tFilterGate;
    private int tFilterChanges;

    public TFilterGate()
    {
        tFilterInspector = TAtelier.TAtelierCreate(port => port with { LAtelierFilter = tFilterPort })
            .CAtelierInspectorCreate();
        tFilterGate = tFilterInspector.CInspectorHighpass;
        tFilterGate.CFilterChanged += () => tFilterChanges++;
    }

    [Fact]
    public void Origin_EachSideItsDefaultPreset()
    {
        Assert.Equal(new double[] { 80, 2, 0.707 }, tFilterGate.CFilterValues);
        Assert.False(tFilterGate.CFilterActive);
        Assert.Equal(2, tFilterGate.CFilterPoles);
        Assert.Equal("Voice", tFilterGate.CFilterToken);
        Assert.Equal(2, tFilterGate.CFilterChoices.ToList().FindIndex(row => row.CChoiceChosen));
        Assert.Equal("Air tame", tFilterInspector.CInspectorLowpass.CFilterToken);
        Assert.Equal(new double[] { 3000, 1, 0.1 }, tFilterInspector.CInspectorLowpass.CFilterLeasts);
    }

    [Fact]
    public void PresetSelect_TakesValues_OneEngineCall()
    {
        tFilterGate.CFilterPresetSelect("Wind");

        Assert.Equal(["preset"], tFilterPort.TFilterCalls);
        Assert.Equal("Wind", tFilterGate.CFilterToken);
        Assert.Equal(60, tFilterGate.CFilterValueRead(0));
        Assert.Equal(4, tFilterGate.CFilterValueRead(1));
        Assert.Equal(1, tFilterChanges);
    }

    [Fact]
    public void PresetSelect_OtherSideOrUnknown_Ignored()
    {
        tFilterGate.CFilterPresetSelect("Telephone");
        tFilterGate.CFilterPresetSelect("Nowhere");

        Assert.Equal("Voice", tFilterGate.CFilterToken);
        Assert.Equal(0, tFilterChanges);
    }

    [Fact]
    public void ValueSet_ClampsAndRounds_KeepsToken_CustomShowsBasis()
    {
        tFilterGate.CFilterValueSet(0, 999);
        tFilterGate.CFilterValueSet(1, 2.6);

        Assert.Equal(300, tFilterGate.CFilterValueRead(0));
        Assert.Equal(3, tFilterGate.CFilterValueRead(1));
        Assert.Equal("Voice", tFilterGate.CFilterToken);
        Assert.Null(tFilterGate.CFilterPreset);
        CChoice custom = tFilterGate.CFilterChoices[^1];
        Assert.True(custom.CChoiceChosen);
        Assert.Equal("Inspector.Common.PresetCustom", custom.CChoiceKey);
        Assert.Equal("Inspector.Pass.Preset.Voice", custom.CChoiceBasis);
        Assert.Equal(80, tFilterGate.CFilterDefaultRead(0));
        Assert.Equal(2, tFilterChanges);
    }

    [Fact]
    public void ValueSet_SlotOutOfRange_NotTold()
    {
        tFilterGate.CFilterValueSet(7, 1);

        Assert.Equal(0, tFilterChanges);
        Assert.Equal(80, tFilterGate.CFilterValueRead(7));
    }

    [Fact]
    public void ChoiceSelect_CustomChosenAndOutOfRange_Ignored()
    {
        tFilterGate.CFilterChoiceSelect(2);
        tFilterGate.CFilterChoiceSelect(tFilterGate.CFilterChoices.Count - 1);
        tFilterGate.CFilterChoiceSelect(-1);

        Assert.Empty(tFilterPort.TFilterCalls);
        Assert.Equal(0, tFilterChanges);
    }

    [Fact]
    public void PolesSelect_ResonanceFollows_OutOfRangeIgnored()
    {
        tFilterGate.CFilterPolesSelect(0);
        tFilterGate.CFilterPolesSelect(2);

        Assert.Equal(1, tFilterGate.CFilterPoles);
        Assert.False(tFilterGate.CFilterResonanceActive);
        Assert.True(tFilterGate.CFilterPoleChoices[0].CChoiceChosen);
        Assert.Equal(1, tFilterChanges);
        Assert.Null(tFilterGate.CFilterPreset);
    }

    [Fact]
    public void SwitchAndPersistence_ToldOnChangeOnly()
    {
        tFilterGate.CFilterActiveSet(true);
        tFilterGate.CFilterActiveSet(true);
        tFilterGate.CFilterPersistentSet(true);
        tFilterGate.CFilterPersistentSet(true);

        Assert.True(tFilterGate.CFilterActive);
        Assert.True(tFilterGate.CFilterPersistent);
        Assert.Equal(2, tFilterChanges);
        Assert.Empty(tFilterPort.TFilterCalls);
    }

    [Fact]
    public void AudioAccept_TakesPlanMatchAsToken()
    {
        var plan = new TInspectorPort
        {
            TInspectorFilter = (true, new List<double> { 60, 4, 0.707 }, 2, "Wind")
        };

        tFilterInspector.CInspectorAudio.CInspectorAudioAccept(plan);

        Assert.True(tFilterGate.CFilterActive);
        Assert.Equal("Wind", tFilterGate.CFilterToken);
        Assert.Contains("highpass", plan.TInspectorCalls);
        Assert.Equal(1, tFilterChanges);
    }
}
