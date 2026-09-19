using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PGroup
{
    private TextBox PGroupEditBuild(LGroupRecord pRecord)
    {
        var pNameBox = new TextBox
        {
            Text = pRecord.LGroupRecordName,
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(2, 0, 2, 0),
            Margin = new Thickness(0, 0, 6, 0)
        };
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };
        pNameBox.KeyDown += (_, pKeyEvent) =>
        {
            if (pKeyEvent.Key == Key.Enter)
            {
                LGroup.LGroupNameCommit(pNameBox.Text);
                pKeyEvent.Handled = true;
            }
            else if (pKeyEvent.Key == Key.Escape)
            {
                LGroup.LGroupEditCancel();
                pKeyEvent.Handled = true;
            }
        };
        pNameBox.LostKeyboardFocus += (_, _) => LGroup.LGroupNameCommit(pNameBox.Text);
        return pNameBox;
    }
}
