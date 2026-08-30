using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSFooter
{
    internal static Button PSFooterButtonBuild(string pText) => new()
    {
        Content = pText,
        Width = 84,
        Height = PSField.PSFieldControlHeight,
        Margin = new Thickness(4),
        Style = PButton.PButtonWhiteCreate()
    };
}
