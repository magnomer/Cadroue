using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TNoisePreset
{
    [Fact]
    public void Rows_InDisplayOrder_CarryFormats()
    {
        QNoise noise = TInterface.TNoiseCreate();
        IReadOnlyList<QInspectorRow> rows = noise.QNoiseRows;

        Assert.Equal(new[] { 0, 2, 1, 4, 3 }, rows.Select(row => row.QInspectorRowIndex));
        Assert.Equal("0.###", rows.Single(row => row.QInspectorRowIndex == 3).QInspectorRowPattern);
        Assert.Equal(
            LGrainCatalog.LGrainFloorLeast, rows.Single(row => row.QInspectorRowIndex == 1).QInspectorRowLeast);
    }

    [Fact]
    public void TypeSelect_MapsIndex_AndReadsBack()
    {
        QNoise noise = TInterface.TNoiseCreate();
        Assert.Equal(0, noise.QNoiseTypeIndex);
        Assert.Equal(3, noise.QNoiseTypeNames.Count);

        TInterface.TNoiseTypeSelect(noise, 2);
        Assert.Equal(LGrain.LGrainShellac, TInterface.TNoiseStepRead(noise).LWorkNoiseType);
        Assert.Equal(2, noise.QNoiseTypeIndex);
        TInterface.TNoiseTypeSelect(noise, 5);
        Assert.Equal(2, noise.QNoiseTypeIndex);
    }

    [Fact]
    public void Choice_MediumByDefault_SelectAndDrift()
    {
        QNoise noise = TInterface.TNoiseCreate();
        QInspectorChoice choice = TInterface.TNoiseChoiceRead(noise);
        Assert.Equal(1, choice.QInspectorChoiceIndex);
        Assert.Equal(LGrainCatalog.LGrainPresets.Count, choice.QInspectorChoiceCustom);

        TInterface.TNoiseChoiceSelect(noise, 2);
        Assert.Equal("Strong", TInterface.TNoiseTokenRead(noise));
        Assert.Equal(24, TInterface.TNoiseValueRead(noise, 0));
        Assert.Equal(24, TInterface.TNoiseDefaultRead(noise, 0));

        TInterface.TNoiseValueSet(noise, 0, 25);
        choice = TInterface.TNoiseChoiceRead(noise);
        Assert.Equal(choice.QInspectorChoiceCustom, choice.QInspectorChoiceIndex);
        Assert.Equal(choice.QInspectorChoiceText, choice.QInspectorChoiceNames[^1]);
    }
}
