using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

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
        QTextbox.QTextboxAttach(pTextBox);
        pTextBox.Padding = new Thickness(6, 0, 10, 0);
        return pTextBox;
    }
}
