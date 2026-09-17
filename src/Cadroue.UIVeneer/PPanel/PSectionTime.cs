using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Core;

namespace Cadroue.UIVeneer.PPanel;

public sealed partial class PSection
{
    private static TimeSpan PSectionSpanRead(LPiece pSectionEntry)
    {
        TimeSpan pSectionSpan = pSectionEntry.LPieceEnd - pSectionEntry.LPieceOrigin;
        return pSectionSpan < TimeSpan.Zero ? TimeSpan.Zero : pSectionSpan;
    }

    private static TextBlock PSectionTimeBuild(string pTimeText) => new()
    {
        Text = pTimeText,
        FontSize = 11,
        FontFamily = pSectionFontFamily,
        Foreground = new SolidColorBrush(Color.FromRgb(0x56, 0x62, 0x73)),
        Background = Brushes.Transparent,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static string PSectionTimeFormat(TimeSpan pTime) =>
        pTime.TotalHours >= 1
            ? $"{(int)pTime.TotalHours}:{pTime.Minutes:D2}:{pTime.Seconds:D2}"
            : $"{pTime.Minutes}:{pTime.Seconds:D2}";
}
