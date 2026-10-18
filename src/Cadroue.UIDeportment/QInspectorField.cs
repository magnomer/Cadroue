using System.Globalization;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

// An inspector number field's text rules: which typed characters it takes, how its text commits to a number and how a
// number shows back in it and on its slider. It holds no state.
public static class QInspectorField
{
    public static bool QInspectorDigitCheck(string lText) => lText.All(char.IsDigit);

    public static bool QInspectorDecimalCheck(string lText) =>
        lText.All(lChar => char.IsDigit(lChar) || lChar == '.' || lChar == '-');

    public static double QInspectorValueCommit(string lText, double lCurrent, double? lLeast, double? lMost)
    {
        double lParsed = double.TryParse(lText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lValue)
            ? lValue
            : lCurrent;
        return lLeast is double lMin && lMost is double lMax ? Math.Clamp(lParsed, lMin, lMax) : lParsed;
    }

    public static string QInspectorValueFormat(string lText, double lNumber, string lFormat)
    {
        string lShown = lNumber.ToString(lFormat, CultureInfo.InvariantCulture);
        bool lSame = lText == lShown
            || (double.TryParse(lText, NumberStyles.Float, CultureInfo.InvariantCulture, out double lParsed)
                && lParsed == lNumber);
        return lSame ? lText : lShown;
    }

    // Typed text that already reads as the number stays as typed; an empty field stays empty at 0.
    internal static string QInspectorWholeFormat(string lText, double lNumber)
    {
        if ((string.IsNullOrWhiteSpace(lText) && lNumber == 0)
            || (double.TryParse(lText, NumberStyles.Integer, CultureInfo.InvariantCulture, out double lShown)
                && lShown == lNumber))
        {
            return lText;
        }

        return Math.Max(0, Math.Round(lNumber)).ToString(CultureInfo.InvariantCulture);
    }

    public static void QInspectorSlideCommit(
        double lSlider, double lCurrent, double lLeast, double lMost, Action<double> lSet)
    {
        if (lSlider != Math.Clamp(lCurrent, lLeast, lMost))
        {
            lSet(lSlider);
        }
    }

    // A row's slider takes the number within its range; its box keeps typed text that already reads as the number.
    internal static void QInspectorFieldUpdate(Slider lSlider, TextBox lValue, double lNumber, string lPattern)
    {
        lSlider.Value = Math.Clamp(lNumber, lSlider.Minimum, lSlider.Maximum);
        lValue.Text = QInspectorValueFormat(lValue.Text, lNumber, lPattern);
    }
}
