using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    private sealed record PInspectorRow(
        Grid PInspectorRowGrid,
        Slider PInspectorRowSlider,
        TextBox PInspectorRowValue,
        TextBlock PInspectorRowUnit,
        int PInspectorRowIndex,
        string PInspectorRowPattern);

    private static TextBox PInspectorNumberBuild()
    {
        TextBox pNumberBox = PInspectorEntryBuild("0");
        QTextbox.QTextboxAttach(pNumberBox);
        PInspectorPaddingApply(pNumberBox);
        PInspectorDigitAttach(pNumberBox);
        return pNumberBox;
    }

    private static TextBox PInspectorDecimalBuild()
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
        Height = PInspectorFieldHeight,
        FontSize = 12,
        FontFamily = pInspectorFontFamily
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

    private static double PInspectorDecimalRead(TextBox pDecimalBox, double pFallback) =>
        QInspectorField.QInspectorValueCommit(pDecimalBox.Text, pFallback, null, null);

    private static void PInspectorTextSet(TextBox pValueBox, double pNumber, string pFormat) =>
        pValueBox.Text = QInspectorField.QInspectorValueFormat(pValueBox.Text, pNumber, pFormat);

    private static void PInspectorSectionUpdate(FrameworkElement pStack, bool pEnabled)
    {
        PInspectorEnabledApply(pStack, pEnabled);
        PInspectorOpacityApply(pStack, pEnabled);
    }

    private static void PInspectorEnabledApply(FrameworkElement pElement, bool pEnabled) =>
        pElement.IsEnabled = pEnabled;

    private static void PInspectorOpacityApply(FrameworkElement pElement, bool pEnabled) =>
        pElement.Opacity = PLook.PLookOpacity[pEnabled];

    private static ComboBox PInspectorChoiceBuild(IReadOnlyList<string> pNames, Action<int> pSelect)
    {
        ComboBox pCombo = PInspectorComboBuild();
        PInspectorChoiceAppend(pCombo, pNames);
        PInspectorChoiceAttach(pCombo, pSelect);
        return pCombo;
    }

    private static void PInspectorChoiceAppend(ComboBox pCombo, IReadOnlyList<string> pNames) =>
        pNames.ToList().ForEach(pName => pCombo.Items.Add(pName));

    private static void PInspectorChoiceAttach(ComboBox pCombo, Action<int> pSelect) =>
        pCombo.SelectionChanged += (_, _) => pSelect(pCombo.SelectedIndex);

    private static void PInspectorChoiceApply(ComboBox pCombo, QInspectorChoice lChoice)
    {
        pCombo.Items[lChoice.QInspectorChoiceCustom] = lChoice.QInspectorChoiceText;
        pCombo.SelectedIndex = lChoice.QInspectorChoiceIndex;
    }

    private static Slider PInspectorSliderBuild(double pLeast, double pMost, double pValue, Func<double> pReset)
    {
        var pSlider = new Slider
        {
            Minimum = pLeast,
            Maximum = pMost,
            Value = pValue,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        QSlider.QSliderResetAttach(pSlider, pReset);
        return pSlider;
    }

    private static TextBlock PInspectorUnitBuild(string pUnit) => new()
    {
        Text = pUnit,
        FontSize = 11,
        FontFamily = pInspectorFontFamily,
        Foreground = pInspectorMutedBrush,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(8, 0, 8, 0)
    };

    private static Grid PInspectorSliderBuild(string pLabel, Slider pSlider, string pUnit, TextBox pValue) =>
        PInspectorSliderBuild(pLabel, pSlider, PInspectorUnitBuild(pUnit), pValue);

    private static Grid PInspectorSliderBuild(string pLabel, Slider pSlider, TextBlock pUnit, TextBox pValue)
    {
        var pRow = new Grid
        {
            Height = PInspectorRowHeight,
            Margin = new Thickness(0, 0, 0, 8)
        };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        TextBlock pLabelBlock = PInspectorLabelBuild(pLabel);
        pSlider.VerticalAlignment = VerticalAlignment.Center;
        pValue.VerticalAlignment = VerticalAlignment.Center;
        Grid.SetColumn(pLabelBlock, 0);
        Grid.SetColumn(pSlider, 1);
        Grid.SetColumn(pUnit, 2);
        Grid.SetColumn(pValue, 3);
        pRow.Children.Add(pLabelBlock);
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pUnit);
        pRow.Children.Add(pValue);
        return pRow;
    }

    private static PInspectorRow PInspectorRowBuild(
        QInspectorRow lRow, Func<double> pRead, Func<double> pReset, Action<double> pSet)
    {
        Slider pSlider = PInspectorSliderBuild(lRow.QInspectorRowLeast, lRow.QInspectorRowMost, pRead(), pReset);
        TextBox pValue = PInspectorDecimalBuild();
        PInspectorTextSet(pValue, pRead(), lRow.QInspectorRowPattern);
        PInspectorValueAttach(pSlider, pValue, lRow.QInspectorRowLeast, lRow.QInspectorRowMost, pRead, pSet);
        TextBlock pUnit = PInspectorUnitBuild(lRow.QInspectorRowUnit);
        Grid pGrid = PInspectorSliderBuild(lRow.QInspectorRowLabel, pSlider, pUnit, pValue);
        return new PInspectorRow(pGrid, pSlider, pValue, pUnit, lRow.QInspectorRowIndex, lRow.QInspectorRowPattern);
    }

    private static void PInspectorRowUpdate(PInspectorRow pRow, double pValue) =>
        PInspectorValueUpdate(pRow.PInspectorRowSlider, pRow.PInspectorRowValue, pValue, pRow.PInspectorRowPattern);
}
