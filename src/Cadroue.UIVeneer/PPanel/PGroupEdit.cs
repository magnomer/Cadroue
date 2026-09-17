using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PGroup
{
    private void PGroupEditStart(int pGroupIndex, Grid pHeaderGrid, LGroupRecord pRecord)
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
        Grid.SetColumn(pNameBox, 0);

        if (pHeaderGrid.Children.Count > 0 && pHeaderGrid.Children[0] is TextBlock pNameLabel)
        {
            pHeaderGrid.Children.Remove(pNameLabel);
        }

        pHeaderGrid.Children.Add(pNameBox);
        pNameBox.Loaded += (_, _) =>
        {
            pNameBox.Focus();
            pNameBox.SelectAll();
        };

        bool pNameCommitted = false;
        void PGroupNameCommit(bool pNameApply)
        {
            if (pNameCommitted)
            {
                return;
            }

            pNameCommitted = true;
            LGroup.LGroupNameSet(pGroupIndex, pNameApply ? pNameBox.Text : string.Empty);
        }

        pNameBox.KeyDown += (_, pKeyEvent) =>
        {
            if (pKeyEvent.Key == Key.Enter)
            {
                PGroupNameCommit(true);
                pKeyEvent.Handled = true;
            }
            else if (pKeyEvent.Key == Key.Escape)
            {
                PGroupNameCommit(false);
                pKeyEvent.Handled = true;
            }
        };
        pNameBox.LostKeyboardFocus += (_, _) => PGroupNameCommit(true);
    }
}
