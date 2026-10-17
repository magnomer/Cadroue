using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static partial class PSFader
{
    private const double PSFaderBitrateTicks = 1000;

    internal static UIElement PSFaderBitrateBuild(
        double pMinimumKbps,
        double pMaximumKbps,
        string pValue,
        TextBox pReadout)
    {
        double pStartKbps = PSFaderBitrateClamp(PSFaderBitrateParse(pValue), pMinimumKbps, pMaximumKbps);
        Slider pSlider = PSFaderKbpsBuild(PSFaderPositionResolve(pStartKbps, pMinimumKbps, pMaximumKbps));
        PSFaderReadoutSet(pReadout, PSFaderBitrateFormat(pStartKbps));
        var pSync = new StrongBox<bool>(false);
        PSFaderBitrateAttach(pSlider, pReadout, pSync, pMinimumKbps, pMaximumKbps);
        return PSFaderRowBuild(pSlider, pReadout);
    }

    private static double PSFaderBitrateClamp(double? pKbps, double pMinimum, double pMaximum) =>
        Math.Clamp(pKbps ?? pMinimum, pMinimum, pMaximum);

    private static Slider PSFaderKbpsBuild(double pPosition)
    {
        var pSlider = new Slider
        {
            Minimum = 0,
            Maximum = PSFaderBitrateTicks,
            SmallChange = 1,
            LargeChange = 50,
            Value = pPosition,
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        return pSlider;
    }

    private static void PSFaderBitrateAttach(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        double pMinimum,
        double pMaximum)
    {
        pSlider.ValueChanged += (_, _) => PSFaderBitrateHandle(pSlider, pReadout, pSync, pMinimum, pMaximum);
        pReadout.TextChanged += (_, _) => PSFaderKbpsHandle(pSlider, pReadout, pSync, pMinimum, pMaximum);
    }

    private static void PSFaderBitrateHandle(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        double pMinimum,
        double pMaximum)
    {
        if (pSync.Value)
        {
            return;
        }

        PSFaderSyncSet(pSync, true);
        PSFaderReadoutSet(pReadout, PSFaderBitrateFormat(PSFaderBitrateResolve(pSlider.Value, pMinimum, pMaximum)));
        PSFaderSyncSet(pSync, false);
    }

    private static void PSFaderKbpsHandle(
        Slider pSlider,
        TextBox pReadout,
        StrongBox<bool> pSync,
        double pMinimum,
        double pMaximum)
    {
        if (pSync.Value || PSFaderBitrateParse(pReadout.Text) is not double pKbps)
        {
            return;
        }

        PSFaderSyncSet(pSync, true);
        PSFaderSliderSet(
            pSlider,
            PSFaderPositionResolve(PSFaderBitrateClamp(pKbps, pMinimum, pMaximum), pMinimum, pMaximum));
        PSFaderSyncSet(pSync, false);
    }

    private static double PSFaderPositionResolve(double pKbps, double pMinimum, double pMaximum) =>
        Math.Log(pKbps / pMinimum) / Math.Log(pMaximum / pMinimum) * PSFaderBitrateTicks;

    private static double PSFaderBitrateResolve(double pPosition, double pMinimum, double pMaximum) =>
        pMinimum * Math.Pow(pMaximum / pMinimum, pPosition / PSFaderBitrateTicks);

    private static double? PSFaderBitrateParse(string pText)
    {
        pText = pText.Trim();
        if (pText.Length == 0)
        {
            return null;
        }

        char pUnit = pText[^1];
        string pNumber = char.IsDigit(pUnit) || pUnit == '.' ? pText : pText[..^1];
        if (!double.TryParse(pNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out double pValue)
            || pValue <= 0)
        {
            return null;
        }

        return pUnit switch
        {
            'k' or 'K' => pValue,
            'm' or 'M' => pValue * 1000,
            _ => pValue / 1000
        };
    }

    private static string PSFaderBitrateFormat(double pKbps)
    {
        double pRounded = Math.Round(pKbps);
        return pRounded >= 1000
            ? (pRounded / 1000).ToString("0.##", CultureInfo.InvariantCulture) + "M"
            : ((long)pRounded).ToString(CultureInfo.InvariantCulture) + "k";
    }
}
