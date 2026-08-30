using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSEntry
{
    internal static TextBox PSEntryBuild(string pText, double pWidth)
    {
        var pTextBox = new TextBox
        {
            Text = pText,
            Width = pWidth,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        PTextbox.PTextboxApply(pTextBox);
        pTextBox.Padding = new Thickness(6, 0, 10, 0);
        return pTextBox;
    }
}
