using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private static TextBox PInspectorNumberBuild()
    {
        var pNumberBox = new TextBox
        {
            Text = "0",
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pNumberBox);
        pNumberBox.TextAlignment = TextAlignment.Center;
        pNumberBox.Padding = new Thickness(4, 0, 4, 0);
        pNumberBox.PreviewTextInput += (_, pNumberEvent) =>
            pNumberEvent.Handled = !LInspector.LInspectorDigitCheck(pNumberEvent.Text);
        return pNumberBox;
    }

    private static TextBox PInspectorDecimalBuild()
    {
        var pDecimalBox = new TextBox
        {
            Width = PInspectorInsetWidth,
            Height = PInspectorFieldHeight,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PTextbox.PTextboxApply(pDecimalBox);
        pDecimalBox.TextAlignment = TextAlignment.Center;
        pDecimalBox.Padding = new Thickness(4, 0, 4, 0);
        pDecimalBox.PreviewTextInput += (_, pDecimalEvent) =>
            pDecimalEvent.Handled = !LInspector.LInspectorDecimalCheck(pDecimalEvent.Text);
        return pDecimalBox;
    }

    private static double PInspectorDecimalRead(TextBox pDecimalBox, double pFallback) =>
        LInspector.LInspectorValueCommit(pDecimalBox.Text, pFallback, null, null);

    private static void PInspectorTextSet(TextBox pValueBox, double pNumber, string pFormat) =>
        pValueBox.Text = LInspector.LInspectorValueFormat(pValueBox.Text, pNumber, pFormat);

    private static void PInspectorSectionUpdate(FrameworkElement pStack, bool pEnabled)
    {
        pStack.IsEnabled = pEnabled;
        pStack.Opacity = PLook.PLookOpacity[pEnabled];
    }

    private static void PInspectorPresetUpdate(
        ComboBox pCombo, string? pMatch, string? pToken, Func<string, string> pKeyRead)
    {
        int pLast = pCombo.Items.Count - 1;
        LLocalizationChoice pCustom = pMatch is null && pToken is not null
            ? new LLocalizationChoice(
                "Custom",
                string.Empty,
                LLocalization.LLocalizationFormat(
                    "Inspector.Common.PresetCustom",
                    LLocalization.LLocalizationTextRead(pKeyRead(pToken))))
            : new LLocalizationChoice("Custom", "Inspector.Common.Custom");
        if (pCombo.Items[pLast]?.ToString() != pCustom.ToString())
        {
            pCombo.Items[pLast] = pCustom;
        }

        int pTarget = pLast;
        for (int pIndex = 0; pIndex < pLast; pIndex++)
        {
            if (pMatch is not null && LLocalizationChoice.LLocalizationChoiceRead(pCombo.Items[pIndex]) == pMatch)
            {
                pTarget = pIndex;
                break;
            }
        }

        if (pCombo.SelectedIndex != pTarget)
        {
            pCombo.SelectedIndex = pTarget;
        }
    }

    private static void PInspectorChoiceUpdate(ComboBox pCombo, string pToken)
    {
        for (int pIndex = 0; pIndex < pCombo.Items.Count; pIndex++)
        {
            if (LLocalizationChoice.LLocalizationChoiceRead(pCombo.Items[pIndex]) == pToken)
            {
                if (pCombo.SelectedIndex != pIndex)
                {
                    pCombo.SelectedIndex = pIndex;
                }

                return;
            }
        }
    }

    private static string? PInspectorPresetRead(ComboBox pCombo, string? pToken, string? pMatch)
    {
        string pSelected = LLocalizationChoice.LLocalizationChoiceRead(pCombo.SelectedItem);
        if (pSelected.Length == 0 || pSelected == "Custom")
        {
            return null;
        }

        return pSelected != pToken || pMatch != pSelected ? pSelected : null;
    }
}
