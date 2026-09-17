using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PInspector
{
    private void PBlankRaise()
    {
        if (!pBlankSuppress)
        {
            PSensorRaise();
        }
    }

    public LDetectorBlank PBlankRead()
    {
        if (!pSensorSections.TryGetValue(LDetectorKind.LDetectorKindBlank, out PSensorSection? pSection))
        {
            return LDetectorBlank.LDetectorBlankCreate();
        }

        double pBlankSaturation = Math.Clamp(
            Math.Sqrt((pBlankWheelX * pBlankWheelX) + (pBlankWheelY * pBlankWheelY)), 0, 1);
        double pBlankHue = pBlankWheelPresent
            ? Math.Atan2(pBlankWheelY, pBlankWheelX) * (180.0 / Math.PI)
            : 0;
        if (pBlankHue < 0)
        {
            pBlankHue += 360;
        }

        return LDetectorBlank.LDetectorBlankClamp(new LDetectorBlank(
            pSection.PSensorApplyBox.IsChecked == true,
            pBlankColor.IsChecked == true ? LDetectorType.LDetectorTypeColor : LDetectorType.LDetectorTypeBlack,
            pBlankHue,
            pBlankSaturation,
            PInspectorDecimalRead(pBlankBrightnessValue, LDetectorBlank.LDetectorBlankValue),
            PInspectorDecimalRead(pBlankToleranceValue, LDetector.LDetectorToleranceRead().LDetectorBoundDefault),
            PInspectorDecimalRead(pBlankCoverageValue, LDetector.LDetectorCoverageRead().LDetectorBoundDefault),
            PInspectorDecimalRead(pBlankMinimumValue, LDetectorBlank.LDetectorBlankGap)));
    }

    public void PBlankApply(LDetectorBlank pBlankStep)
    {
        if (!pSensorSections.TryGetValue(LDetectorKind.LDetectorKindBlank, out PSensorSection? pSection))
        {
            return;
        }

        LDetectorBlank pBlank = LDetectorBlank.LDetectorBlankClamp(pBlankStep);
        pBlankSuppress = true;
        pSection.PSensorApplyBox.IsChecked = pBlank.LDetectorBlankEnabled;
        pBlankColor.IsChecked = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeColor;
        pBlankBlack.IsChecked = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeBlack;
        pBlankWheelX = pBlank.LDetectorBlankSaturation * Math.Cos(pBlank.LDetectorBlankHue * (Math.PI / 180.0));
        pBlankWheelY = pBlank.LDetectorBlankSaturation * Math.Sin(pBlank.LDetectorBlankHue * (Math.PI / 180.0));
        pBlankWheelPresent = pBlank.LDetectorBlankType == LDetectorType.LDetectorTypeColor;
        pBlankBrightnessValue.Text = pBlank.LDetectorBlankBrightness.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankToleranceValue.Text = pBlank.LDetectorBlankTolerance.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankCoverageValue.Text = pBlank.LDetectorBlankCoverage.ToString("0.00", CultureInfo.InvariantCulture);
        pBlankMinimumValue.Text = pBlank.LDetectorBlankMinimum.ToString("0.0", CultureInfo.InvariantCulture);
        pBlankSuppress = false;
        PBlankWheelPlace();
        PSensorStackUpdate(pSection);
    }
}
