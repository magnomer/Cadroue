using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PSShared;

internal static class PSPlate
{
    internal static UIElement PSPlateBuild(UIElement pContent) =>
        new StackPanel
        {
            Margin = new Thickness(0, 0, 0, 18),
            Children = { pContent }
        };

    internal static UIElement PSPlateBuild(string pTitle, params UIElement[] pRows)
    {
        var pPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 18) };
        pPanel.Children.Add(new TextBlock
        {
            Text = pTitle,
            Foreground = PSField.PSFieldText,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10)
        });

        foreach (UIElement pRow in pRows)
        {
            pPanel.Children.Add(pRow);
        }

        return pPanel;
    }
}
