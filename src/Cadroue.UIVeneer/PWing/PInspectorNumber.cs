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
            pNumberEvent.Handled = !QInspector.QInspectorDigitCheck(pNumberEvent.Text);
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
            pDecimalEvent.Handled = !QInspector.QInspectorDecimalCheck(pDecimalEvent.Text);
        return pDecimalBox;
    }

    private static double PInspectorDecimalRead(TextBox pDecimalBox, double pFallback) =>
        QInspector.QInspectorValueCommit(pDecimalBox.Text, pFallback, null, null);

    private static void PInspectorTextSet(TextBox pValueBox, double pNumber, string pFormat) =>
        pValueBox.Text = QInspector.QInspectorValueFormat(pValueBox.Text, pNumber, pFormat);

    private static void PInspectorSectionUpdate(FrameworkElement pStack, bool pEnabled)
    {
        pStack.IsEnabled = pEnabled;
        pStack.Opacity = PLook.PLookOpacity[pEnabled];
    }

    private static ComboBox PInspectorChoiceBuild(IReadOnlyList<string> pNames, Action<int> pSelect)
    {
        var pCombo = new ComboBox
        {
            Height = PInspectorFieldHeight,
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontSize = 12,
            FontFamily = pInspectorFontFamily
        };
        PDropdown.PDropdownApply(pCombo);
        pNames.ToList().ForEach(pName => pCombo.Items.Add(pName));
        pCombo.SelectionChanged += (_, _) => pSelect(pCombo.SelectedIndex);
        return pCombo;
    }

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
        PSlider.PSliderApply(pSlider);
        PSlider.PSliderResetApply(pSlider, pReset);
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
