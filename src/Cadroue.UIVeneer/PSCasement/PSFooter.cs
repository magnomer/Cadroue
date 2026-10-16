using System.Windows;
using System.Windows.Controls;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSFooter
{
    internal static Button PSFooterButtonBuild(string pText) => QButton.QButtonBuild(
        new()
        {
            Content = pText,
            Width = 84,
            Height = PSField.PSFieldControlHeight,
            Margin = new Thickness(4)
        },
        QButtonLook.QButtonLookWhite);
}
