using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Preset")]
public sealed class TEditTabColor
{
    private static (LEditTab, LInspector) TEditBuild() => TEditBuild(TInterface.TViewerCreate());

    private static (LEditTab, LInspector) TEditBuild(LViewer viewer)
    {
        LInspector inspector = TInterface.TInspectorCreate();
        LDocket docket = TInterface.TDocketCreate();
        LProcessing processing = TInterface.TProcessingCreate();
        foreach (LProcessingRow row in LEditTab.LEditRows)
        {
            TInterface.TProcessingStepAdd(processing, row.LProcessingRowKey);
        }

        LEditTab tab = TInterface.TEditTabCreate(
            TInterface.TPresetSelectionCreate("Alpha"),
            inspector,
            viewer,
            TInterface.TListCreate(docket),
            docket,
            processing);
        return (tab, inspector);
    }

    [Fact]
    public void HistogramApply_Frame_ReachesCurveBins()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector) = TEditBuild();
        byte[] pixels = new byte[4 * 4];
        for (int index = 0; index < 4; index++)
        {
            pixels[index * 4] = 255;
            pixels[index * 4 + 3] = 255;
        }

        TInterface.TEditHistogramApply(tab, 2, 2, pixels);

        LHistogramCounts? counts = inspector.LInspectorCurve.LCurveHistogram;
        Assert.NotNull(counts);
        Assert.Equal(4, counts!.LHistogramRed[255]);
        Assert.Equal(4, counts.LHistogramGreen[0]);
        Assert.Equal(4, counts.LHistogramRed.Sum());

        TInterface.TEditHistogramApply(tab, 0, 0, null);

        Assert.Null(inspector.LInspectorCurve.LCurveHistogram);
    }

    [Fact]
    public void NeutralHandle_ValidSample_SetsManualGains()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (LEditTab tab, LInspector inspector) = TEditBuild();

        TInterface.TEditNeutralHandle(tab, TInterface.TNeutralSampleCreate(120, 128, 136, 1.1, 1.0, 0.9));

        LWorkWhitebalanceSettings value = TInterface.TWhitebalanceValueRead(inspector.LInspectorWhitebalance);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodManual, value.LWorkWhitebalanceMethod);
        Assert.Equal(1.1, value.LWorkWhitebalanceRed);
        Assert.Equal(0.9, value.LWorkWhitebalanceBlue);
        Assert.True(inspector.LInspectorWhitebalance.LWhitebalanceStep.LWorkStepActive);
    }

    [Fact]
    public void EstimateHandle_ManualStaysQuiet_AutomaticAsksViewer()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        LViewer viewer = TInterface.TViewerCreate();
        (LEditTab tab, LInspector inspector) = TEditBuild(viewer);
        var asked = new List<LNeutralWheel>();
        TInterface.TViewerEstimateAttach(TInterface.TViewerNeutralRead(viewer), asked.Add);

        TInterface.TEditEstimateHandle(tab, LWhitebalanceMethod.LWhitebalanceMethodManual);
        TInterface.TEditEstimateHandle(tab, LWhitebalanceMethod.LWhitebalanceMethodMedian);
        TInterface.TEditEstimateApply(tab, 0.25, -0.5, true);

        Assert.Single(asked);
        Assert.False(asked[0].LNeutralWheelPresent);
        LNeutralWheel wheel = TInterface.TWhitebalanceWheelRead(inspector.LInspectorWhitebalance);
        Assert.True(wheel.LNeutralWheelPresent);
        Assert.Equal(0.25, wheel.LNeutralWheelX);
    }

    [Fact]
    public void ColorApply_SkipActive_PushesNeutralColor()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        LViewer viewer = TInterface.TViewerCreate();
        (LEditTab tab, LInspector inspector) = TEditBuild(viewer);
        TInterface.TToneValueSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, 40);
        TInterface.TToneActiveSet(inspector.LInspectorTone, LColorKind.LColorKindBrightness, true);
        var colors = new List<LColor>();
        TInterface.TEditPreviewAttach(viewer, colors.Add);

        TInterface.TEditColorApply(tab);
        TInterface.TSkipActiveSet(inspector.LInspectorSkip, true);

        Assert.Equal(3, colors.Count);
        Assert.NotEqual(0, colors[0].LColorBrightness);
        Assert.Equal(TInterface.TPreviewColorResolve(TInterface.TWorkVideoCreate()), colors[^1]);
    }
}
