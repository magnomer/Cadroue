using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TNoisePreset
{
    [Fact]
    public void Rows_InDisplayOrder_CarryFormats()
    {
        LNoise noise = TInterface.TNoiseCreate();
        IReadOnlyList<LInspectorRow> rows = noise.LNoiseRows;

        Assert.Equal(new[] { 0, 2, 1, 4, 3 }, rows.Select(row => row.LInspectorRowIndex));
        Assert.Equal("0.###", rows.Single(row => row.LInspectorRowIndex == 3).LInspectorRowPattern);
        Assert.Equal(
            LGrainCatalog.LGrainFloorLeast, rows.Single(row => row.LInspectorRowIndex == 1).LInspectorRowLeast);
    }

    [Fact]
    public void TypeSelect_MapsIndex_AndReadsBack()
    {
        LNoise noise = TInterface.TNoiseCreate();
        Assert.Equal(0, noise.LNoiseTypeIndex);
        Assert.Equal(3, noise.LNoiseTypeNames.Count);

        TInterface.TNoiseTypeSelect(noise, 2);
        Assert.Equal(LGrain.LGrainShellac, TInterface.TNoiseStepRead(noise).LWorkNoiseType);
        Assert.Equal(2, noise.LNoiseTypeIndex);
        TInterface.TNoiseTypeSelect(noise, 5);
        Assert.Equal(2, noise.LNoiseTypeIndex);
    }

    [Fact]
    public void Choice_MediumByDefault_SelectAndDrift()
    {
        LNoise noise = TInterface.TNoiseCreate();
        LInspectorChoice choice = TInterface.TNoiseChoiceRead(noise);
        Assert.Equal(1, choice.LInspectorChoiceIndex);
        Assert.Equal(LGrainCatalog.LGrainPresets.Count, choice.LInspectorChoiceCustom);

        TInterface.TNoiseChoiceSelect(noise, 2);
        Assert.Equal("Strong", TInterface.TNoiseTokenRead(noise));
        Assert.Equal(24, TInterface.TNoiseValueRead(noise, 0));
        Assert.Equal(24, TInterface.TNoiseDefaultRead(noise, 0));

        TInterface.TNoiseValueSet(noise, 0, 25);
        choice = TInterface.TNoiseChoiceRead(noise);
        Assert.Equal(choice.LInspectorChoiceCustom, choice.LInspectorChoiceIndex);
        Assert.Equal(choice.LInspectorChoiceText, choice.LInspectorChoiceNames[^1]);
    }
}
