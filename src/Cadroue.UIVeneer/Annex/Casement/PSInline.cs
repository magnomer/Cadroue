using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSInline
{
    internal static Button PSInlineButtonBuild(string pText, double pWidth, Thickness pMargin) =>
        QButton.QButtonBuild(
            new()
            {
                Content = pText,
                Width = pWidth,
                Height = PSField.PSFieldControlHeight,
                Margin = pMargin
            },
            QButtonLook.QButtonLookWhite);

    internal static Button PSInlineIconBuild(string pIconPath, string pTooltip, Thickness pMargin) =>
        QButton.QButtonBuild(
            new()
            {
                Content = new Image
                {
                    Source = QIcon.QIconRead(pIconPath, PSField.PSFieldText),
                    Width = 22,
                    Height = 22,
                    Stretch = Stretch.Uniform
                },
                Width = 40,
                Height = PSField.PSFieldControlHeight,
                Padding = new Thickness(0),
                Margin = pMargin,
                ToolTip = pTooltip
            },
            QButtonLook.QButtonLookWhite);
}
