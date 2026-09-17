using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private (RadioButton, RadioButton) PSensorMetricBuild(StackPanel pStack)
    {
        string pGroup = "PSensorVolumeMetric_" + Guid.NewGuid().ToString("N");
        RadioButton pLufs = PSensorRadioBuild(
            "Inspector.Metric.Lufs",
            pGroup,
            () => LSensor.LSensorMetricSet(LDetectorMetricMode.LDetectorMetricLufs));
        RadioButton pRms = PSensorRadioBuild(
            "Inspector.Metric.Rms",
            pGroup,
            () => LSensor.LSensorMetricSet(LDetectorMetricMode.LDetectorMetricRms));
        Border pMetricRow = PRadio.PRadioSegmentBuild(pLufs, pRms);
        pStack.Children.Insert(0, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.Metric"), pMetricRow, true));
        return (pLufs, pRms);
    }
}
