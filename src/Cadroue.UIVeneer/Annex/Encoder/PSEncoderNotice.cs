using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal static class PSEncoderNotice
{
    internal static TextBlock PSEncoderErrorBuild() => new()
    {
        Foreground = new SolidColorBrush(Color.FromRgb(0xC0, 0x2A, 0x2A)),
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };

    internal static TextBlock PSEncoderNoticeBuild() => new()
    {
        Foreground = PSField.PSFieldMuted,
        TextWrapping = TextWrapping.Wrap,
        Margin = PSNotice.PSNoticeMargin,
        Visibility = Visibility.Collapsed
    };
}
