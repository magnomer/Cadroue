using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed record PInspectorRow(
    Grid PInspectorRowGrid,
    Slider PInspectorRowSlider,
    TextBox PInspectorRowValue,
    TextBlock PInspectorRowUnit,
    int PInspectorRowIndex,
    string PInspectorRowPattern)
{
    internal static PInspectorRow PInspectorRowBuild(
        QInspectorRow lRow, Func<double> pRead, Func<double> pReset, Action<double> pSet)
    {
        Slider pSlider = PInspectorValue.PInspectorSliderBuild(
            lRow.QInspectorRowLeast, lRow.QInspectorRowMost, pRead(), pReset);
        TextBox pValue = PInspectorNumber.PInspectorDecimalBuild();
        PInspectorNumber.PInspectorTextSet(pValue, pRead(), lRow.QInspectorRowPattern);
        PInspectorValue.PInspectorValueAttach(
            pSlider, pValue, lRow.QInspectorRowLeast, lRow.QInspectorRowMost, pRead, pSet);
        TextBlock pUnit = PInspectorField.PInspectorUnitBuild(lRow.QInspectorRowUnit);
        Grid pGrid = PInspectorField.PInspectorSliderBuild(lRow.QInspectorRowLabel, pSlider, pUnit, pValue);
        return new PInspectorRow(pGrid, pSlider, pValue, pUnit, lRow.QInspectorRowIndex, lRow.QInspectorRowPattern);
    }

    internal void PInspectorRowUpdate(double pValue) =>
        PInspectorValue.PInspectorValueUpdate(PInspectorRowSlider, PInspectorRowValue, pValue, PInspectorRowPattern);
}
