using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSInline
{
    internal static Button PSInlineButtonBuild(string pText, double pWidth, Thickness pMargin) => new()
    {
        Content = pText,
        Width = pWidth,
        Height = PSField.PSFieldControlHeight,
        Margin = pMargin,
        Style = PButton.PButtonWhiteCreate()
    };

    internal static Button PSInlineIconBuild(string pIconPath, string pTooltip, Thickness pMargin) => new()
    {
        Content = new Image
        {
            Source = PAssets.PIcon.PIconRead(pIconPath, PSField.PSFieldText),
            Width = 16,
            Height = 16,
            Stretch = Stretch.Uniform
        },
        Width = 40,
        Height = PSField.PSFieldControlHeight,
        Padding = new Thickness(0),
        Margin = pMargin,
        ToolTip = pTooltip,
        Style = PButton.PButtonWhiteCreate()
    };
}
