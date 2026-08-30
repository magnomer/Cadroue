using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIShell.PSShared;

internal static class PSNotice
{
    internal static Thickness PSNoticeMargin => new(PSField.PSFieldLabelWidth, -7, 0, 9);

    internal static UIElement PSNoticeBuild(string pText) => new TextBlock
    {
        Text = pText,
        Foreground = PSField.PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNoticeMargin
    };
}
