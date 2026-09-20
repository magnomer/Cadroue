using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TLoudnessPreset
{
    [Fact]
    public void Rows_ThreeLoudness_FourDynamic()
    {
        LLoudness loudness = TInterface.TLoudnessCreate();

        Assert.Equal(3, TInterface.TLoudnessRowsRead(loudness, false).Count);
        Assert.Equal(4, TInterface.TLoudnessRowsRead(loudness, true).Count);
        Assert.Equal("LUFS", TInterface.TLoudnessRowsRead(loudness, false)[0].LInspectorRowUnit);
        Assert.Equal(
            LLevelingCatalog.LLevelingFrameMost, TInterface.TLoudnessRowsRead(loudness, true)[0].LInspectorRowMost);
    }

    [Fact]
    public void ModeSelect_SwitchesIndex_AndChoiceFamily()
    {
        LLoudness loudness = TInterface.TLoudnessCreate();
        Assert.Equal(0, loudness.LLoudnessModeIndex);

        TInterface.TLoudnessModeSelect(loudness, 1);
        Assert.True(loudness.LLoudnessDynamic);
        Assert.Equal(1, loudness.LLoudnessModeIndex);
        Assert.Equal(
            LLevelingCatalog.LLevelingDynamicTokens.Count + 1,
            TInterface.TLoudnessChoiceRead(loudness, true).LInspectorChoiceNames.Count);
        Assert.Equal(
            LLevelingCatalog.LLevelingLoudnessTokens.Count + 1,
            TInterface.TLoudnessChoiceRead(loudness, false).LInspectorChoiceNames.Count);
        Assert.Equal(2, loudness.LLoudnessModeNames.Count);
    }

    [Fact]
    public void ChoiceSelect_OnlyForActiveMode()
    {
        LLoudness loudness = TInterface.TLoudnessCreate();

        TInterface.TLoudnessChoiceSelect(loudness, true, 0);
        Assert.Equal(TInterface.TLoudnessMatchRead(loudness), TInterface.TLoudnessTokenRead(loudness));

        TInterface.TLoudnessChoiceSelect(loudness, false, 2);
        Assert.Equal(LLevelingCatalog.LLevelingLoudnessTokens[2], TInterface.TLoudnessTokenRead(loudness));
        Assert.Equal(2, TInterface.TLoudnessChoiceRead(loudness, false).LInspectorChoiceIndex);
    }

    [Fact]
    public void ValueSet_ByModeAndIndex_DefaultsFollowToken()
    {
        LLoudness loudness = TInterface.TLoudnessCreate();

        TInterface.TLoudnessValueSet(loudness, false, 1, -3);
        Assert.Equal(-3, TInterface.TLoudnessValueRead(loudness, false, 1));
        TInterface.TLoudnessValueSet(loudness, true, 2, 12);
        Assert.Equal(12, TInterface.TLoudnessValueRead(loudness, true, 2));

        TInterface.TLoudnessChoiceSelect(loudness, false, 0);
        LLevelingLoudnessPreset preset = LLevelingCatalog.LLevelingLoudnessPresets[0];
        Assert.Equal(preset.LLevelingTarget, TInterface.TLoudnessDefaultRead(loudness, false, 0));
        Assert.Equal(preset.LLevelingPeak, TInterface.TLoudnessDefaultRead(loudness, false, 1));
        Assert.Equal(
            TInterface.TLevelingDefaultRead().LLevelingFrame, TInterface.TLoudnessDefaultRead(loudness, true, 0));
    }
}
