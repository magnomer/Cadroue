using System.Windows.Controls;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// An inspector number field's control update and the driver relays kept for surfaces that cannot name Conduct.
public static class QInspectorField
{
    public static bool QInspectorDigitCheck(string lText) => CInspectorField.CInspectorDigitCheck(lText);

    public static bool QInspectorDecimalCheck(string lText) =>
        CInspectorField.CInspectorDecimalCheck(lText);

    public static double QInspectorValueCommit(string lText, double lCurrent, double? lLeast, double? lMost) =>
        CInspectorField.CInspectorValueCommit(lText, lCurrent, lLeast, lMost);

    public static string QInspectorValueFormat(string lText, double lNumber, string lFormat) =>
        CInspectorField.CInspectorValueFormat(lText, lNumber, lFormat);

    internal static string QInspectorWholeFormat(string lText, double lNumber) =>
        CInspectorField.CInspectorWholeFormat(lText, lNumber);

    // A row's slider takes the number within its range; its box keeps typed text that already reads as the number.
    internal static void QInspectorFieldUpdate(Slider lSlider, TextBox lValue, double lNumber, string lPattern)
    {
        lSlider.Value = Math.Clamp(lNumber, lSlider.Minimum, lSlider.Maximum);
        lValue.Text = CInspectorField.CInspectorValueFormat(lValue.Text, lNumber, lPattern);
    }
}
