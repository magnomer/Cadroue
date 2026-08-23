using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PInspector
{
    private RadioButton PSensorMetricBuild(StackPanel pStack, Action pSensorRaise, TextBlock? pThresholdUnit)
    {
        string pMetricGroup = "PSensorVolumeMetric_" + System.Guid.NewGuid().ToString("N");
        var pMetricLufs = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Metric.Lufs"),
            GroupName = pMetricGroup,
            IsChecked = true,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        var pMetricRms = new RadioButton
        {
            Content = LLocalization.LLocalizationTextRead("Inspector.Metric.Rms"),
            GroupName = pMetricGroup,
            FontSize = 12,
            FontFamily = pInspectorFontFamily,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        pMetricLufs.Checked += (_, _) =>
        {
            if (pThresholdUnit is not null)
            {
                pThresholdUnit.Text = "LU";
            }

            PSensorPresetSync(LDetectorKind.LDetectorKindVolume);
            pSensorRaise();
        };
        pMetricRms.Checked += (_, _) =>
        {
            if (pThresholdUnit is not null)
            {
                pThresholdUnit.Text = "dB";
            }

            PSensorPresetSync(LDetectorKind.LDetectorKindVolume);
            pSensorRaise();
        };

        Border pMetricRow = PRadio.PRadioSegmentBuild(pMetricLufs, pMetricRms);
        pStack.Children.Insert(0, PInspectorFieldBuild(
            LLocalization.LLocalizationTextRead("Inspector.Detector.Metric"), pMetricRow, true));
        return pMetricRms;
    }

    public LDetectorMetricMode PSensorMetricRead(LDetectorKind pDetectorKind)
    {
        if (pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection)
            && pSection.PSensorMetric is { IsChecked: true })
        {
            return LDetectorMetricMode.LDetectorMetricRms;
        }

        return LDetectorMetricMode.LDetectorMetricLufs;
    }

    public void PSensorMetricApply(LDetectorKind pDetectorKind, LDetectorMetricMode pDetectorMode)
    {
        if (!pSensorSections.TryGetValue(pDetectorKind, out PSensorSection? pSection)
            || pSection.PSensorMetric is not { } pMetricRms)
        {
            return;
        }

        pSection.PSensorSuppress = true;
        if (pDetectorMode == LDetectorMetricMode.LDetectorMetricRms)
        {
            pMetricRms.IsChecked = true;
        }
        else if (pMetricRms.Parent is Panel pMetricRow && pMetricRow.Children[0] is RadioButton pMetricLufs)
        {
            pMetricLufs.IsChecked = true;
        }

        pSection.PSensorSuppress = false;
    }
}
