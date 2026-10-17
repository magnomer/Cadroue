using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TNoisePreset
{
    [Fact]
    public void Rows_InDisplayOrder_CarryFormats()
    {
        IReadOnlyList<QInspectorRow> rows = new QNoise(TInterface.TNoiseCreate()).QNoiseRows;

        Assert.Equal(new[] { 0, 1, 2, 3, 4 }, rows.Select(row => row.QInspectorRowIndex));
        Assert.Equal("0.###", rows.Single(row => row.QInspectorRowIndex == 4).QInspectorRowPattern);
        Assert.Equal(
            LGrainCatalog.LGrainFloorLeast, rows.Single(row => row.QInspectorRowIndex == 2).QInspectorRowLeast);
    }

    [Fact]
    public void TypeSelect_MapsIndex_AndReadsBack()
    {
        CNoise noise = TInterface.TNoiseCreate();
        Assert.Equal(0, noise.CNoiseType);
        Assert.Equal(3, new QNoise(noise).QNoiseTypeNames.Count);

        TInterface.TNoiseTypeSelect(noise, 2);
        Assert.Equal(LGrain.LGrainShellac, TInterface.TNoiseStepRead(noise).LWorkNoiseType);
        Assert.Equal(2, noise.CNoiseType);
        TInterface.TNoiseTypeSelect(noise, 5);
        Assert.Equal(2, noise.CNoiseType);
    }

    [Fact]
    public void Choice_MediumByDefault_SelectAndDrift()
    {
        CNoise noise = TInterface.TNoiseCreate();
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
