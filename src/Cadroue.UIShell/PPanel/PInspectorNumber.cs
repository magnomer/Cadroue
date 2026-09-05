using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PHouse;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PInspector
{
    private TextBox PCropFieldBuild()
    {
        TextBox pRatioBox = PInspectorNumberBuild();
        pRatioBox.TextChanged += (_, _) => PCropRatioHandle();
        return pRatioBox;
    }

    private TextBox PInspectorInsetBuild(int pEdge)
    {
        TextBox pInsetBox = PInspectorNumberBuild();
        pInsetBox.TextChanged += (_, _) => PInspectorInsetChange(pEdge);
        return pInsetBox;
    }

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
            pNumberEvent.Handled = !pNumberEvent.Text.All(char.IsDigit);
        return pNumberBox;
    }
}
