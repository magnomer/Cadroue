using System;
using System.Globalization;
using System.Linq;

namespace Cadroue.Conduct;

// The medium-free decisions of an inspector number field: accepted typed characters, parsing and clamping a commit,
// preserving equivalent typed text, formatting whole values, and deciding whether a slider change commits.
public static class CInspectorField
{
    public static bool CInspectorDigitCheck(string text) => text.All(char.IsDigit);

    public static bool CInspectorDecimalCheck(string text) =>
        text.All(character => char.IsDigit(character) || character == '.' || character == '-');

    public static double CInspectorValueCommit(string text, double current, double? least, double? most)
    {
        double parsed = double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
            ? value
            : current;
        return least is double minimum && most is double maximum ? Math.Clamp(parsed, minimum, maximum) : parsed;
    }

    public static string CInspectorValueFormat(string text, double number, string format)
    {
        string shown = number.ToString(format, CultureInfo.InvariantCulture);
        bool same = text == shown
            || (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                && parsed == number);
        return same ? text : shown;
    }

    // Typed text that already reads as the number stays as typed; an empty field stays empty at 0.
    public static string CInspectorWholeFormat(string text, double number)
    {
        if ((string.IsNullOrWhiteSpace(text) && number == 0)
            || (double.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out double shown)
                && shown == number))
        {
            return text;
        }

        return Math.Max(0, Math.Round(number)).ToString(CultureInfo.InvariantCulture);
    }

    public static void CInspectorSlideCommit(
        double slider, double current, double least, double most, Action<double> set)
    {
        if (slider != Math.Clamp(current, least, most))
        {
            set(slider);
        }
    }
}
