using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal static class PInspectorNumber
{
    private const double PInspectorInsetWidth = 68;

    internal static TextBox PInspectorNumberBuild()
    {
        TextBox pNumberBox = PInspectorEntryBuild("0");
        QTextbox.QTextboxAttach(pNumberBox);
        PInspectorPaddingApply(pNumberBox);
        PInspectorDigitAttach(pNumberBox);
        return pNumberBox;
    }

    internal static TextBox PInspectorDecimalBuild()
    {
        TextBox pDecimalBox = PInspectorEntryBuild(string.Empty);
        QTextbox.QTextboxAttach(pDecimalBox);
        PInspectorPaddingApply(pDecimalBox);
        PInspectorDecimalAttach(pDecimalBox);
        return pDecimalBox;
    }

    private static TextBox PInspectorEntryBuild(string pText) => new()
    {
        Text = pText,
        Width = PInspectorInsetWidth,
        Height = PInspectorLook.PInspectorFieldHeight,
        FontSize = 12,
        FontFamily = PInspectorLook.PInspectorFontFamily
    };

    private static void PInspectorPaddingApply(TextBox pEntryBox)
    {
        pEntryBox.TextAlignment = TextAlignment.Center;
        pEntryBox.Padding = new Thickness(4, 0, 4, 0);
    }

    private static void PInspectorDigitAttach(TextBox pNumberBox) =>
        pNumberBox.PreviewTextInput += (_, pNumberEvent) =>
            pNumberEvent.Handled = !QInspectorField.QInspectorDigitCheck(pNumberEvent.Text);

    private static void PInspectorDecimalAttach(TextBox pDecimalBox) =>
        pDecimalBox.PreviewTextInput += (_, pDecimalEvent) =>
            pDecimalEvent.Handled = !QInspectorField.QInspectorDecimalCheck(pDecimalEvent.Text);

    internal static void PInspectorTextSet(TextBox pValueBox, double pNumber, string pFormat) =>
        pValueBox.Text = QInspectorField.QInspectorValueFormat(pValueBox.Text, pNumber, pFormat);

    internal static void PInspectorCaretPlace(TextBox pValue) => pValue.CaretIndex = pValue.Text.Length;
}
