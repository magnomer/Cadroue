using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TEditTabColor
{
    private static (QEditTab, QInspector) TEditBuild() => TEditBuild(TInterfaceViewer.TViewerCreate());

    private static (QEditTab, QInspector) TEditBuild(QViewer viewer)
    {
        QList list = TInterfaceList.TListCreate();
        QEditTab tab = TInterface.TEditTabCreate(TInterface.TPresetSelectionCreate("Alpha"), viewer, list);
        QInspector inspector = TInterface.TEditInspectorRead(tab);
        QProcessing processing = TInterface.TEditProcessingRead(tab);
        foreach (QProcessingRow row in QEditProcessing.QEditRows)
        {
            TInterface.TProcessingStepAdd(processing, row.QProcessingRowKey);
        }
        return (tab, inspector);
    }

    [Fact]
    public void HistogramApply_Frame_ReachesCurveBins()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector) = TEditBuild();
        byte[] pixels = new byte[4 * 4];
        for (int index = 0; index < 4; index++)
        {
            pixels[index * 4] = 255;
            pixels[index * 4 + 3] = 255;
        }

        TInterface.TEditHistogramApply(tab, 2, 2, pixels);

        LHistogramCounts? counts = inspector.QInspectorVideo.QInspectorCurve.QCurveHistogram;
        Assert.NotNull(counts);
        Assert.Equal(4, counts!.LHistogramRed[255]);
        Assert.Equal(4, counts.LHistogramGreen[0]);
        Assert.Equal(4, counts.LHistogramRed.Sum());

        TInterface.TEditHistogramApply(tab, 0, 0, null);

        Assert.Null(inspector.QInspectorVideo.QInspectorCurve.QCurveHistogram);
    }

    [Fact]
    public void NeutralHandle_ValidSample_SetsManualGains()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        (QEditTab tab, QInspector inspector) = TEditBuild();

        TInterface.TEditNeutralHandle(tab, TInterface.TNeutralSampleCreate(120, 128, 136, 1.1, 1.0, 0.9));

        LWorkWhitebalanceSettings value =
            TInterface.TWhitebalanceValueRead(inspector.QInspectorVideo.QInspectorWhitebalance);
        Assert.Equal(LWhitebalanceMethod.LWhitebalanceMethodManual, value.LWorkWhitebalanceMethod);
        Assert.Equal(1.1, value.LWorkWhitebalanceRed);
        Assert.Equal(0.9, value.LWorkWhitebalanceBlue);
        Assert.True(inspector.QInspectorVideo.QInspectorWhitebalance.CWhitebalanceActive);
    }

    [Fact]
    public void EstimateHandle_AutomaticAsksViewer_ManualStaysQuiet()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        (QEditTab tab, QInspector inspector) = TEditBuild(viewer);
        var asked = new List<LNeutralWheel>();
        TInterfaceViewer.TViewerEstimateAttach(TInterfaceViewer.TViewerNeutralRead(viewer), asked.Add);

        TInterface.TEditEstimateHandle(tab, "Median");
        TInterface.TEditEstimateApply(tab, 0.25, -0.5, true);
        LNeutralWheel wheel = TInterface.TWhitebalanceWheelRead(inspector.QInspectorVideo.QInspectorWhitebalance);
        TInterface.TWhitebalanceMethodSet(
            inspector.QInspectorVideo.QInspectorWhitebalance, LWhitebalanceMethod.LWhitebalanceMethodManual);
        TInterface.TWhitebalanceEstimateStart(inspector.QInspectorVideo.QInspectorWhitebalance);

        Assert.Single(asked);
        Assert.False(asked[0].LNeutralWheelPresent);
        Assert.True(wheel.LNeutralWheelPresent);
        Assert.Equal(0.25, wheel.LNeutralWheelX);
    }

    [Fact]
    public void ColorApply_SkipActive_PushesNeutralColor()
    {
        using TPreset presets = new();
        presets.TPresetSeedCreate("Alpha");
        QViewer viewer = TInterfaceViewer.TViewerCreate();
        (QEditTab tab, QInspector inspector) = TEditBuild(viewer);
        TInterface.TToneValueSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, 40);
        TInterface.TToneActiveSet(inspector.QInspectorVideo.QInspectorTone, LColorKind.LColorKindBrightness, true);
        var colors = new List<LColor>();
        TInterface.TEditPreviewAttach(viewer, colors.Add);

        TInterface.TEditColorApply(tab);
        TInterface.TSkipActiveSet(inspector.QInspectorSkip, true);

        Assert.Equal(3, colors.Count);
        Assert.NotEqual(0, colors[0].LColorBrightness);
        Assert.Equal(Interface.TInterface.TPreviewColorResolve(Interface.TInterface.TWorkVideoCreate()), colors[^1]);
    }
}
