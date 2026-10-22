using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSectionPalette
{
    private const byte PSectionBandAlpha = 0x99;
    private const byte PSectionBadgeAlpha = 0xFF;

    internal static Brush PSectionBandRead(int pColorIndex) =>
        PSectionBrushBuild(QSection.QSectionHexRead(pColorIndex), PSectionBandAlpha);

    internal static Brush PSectionBadgeRead(int pColorIndex) =>
        PSectionBrushBuild(QSection.QSectionHexRead(pColorIndex), PSectionBadgeAlpha);

    private static Brush PSectionBrushBuild(string pHex, byte pAlpha)
    {
        var pColor = (Color)ColorConverter.ConvertFromString(pHex);
        var pBrush = new SolidColorBrush(Color.FromArgb(pAlpha, pColor.R, pColor.G, pColor.B));
        pBrush.Freeze();
        return pBrush;
    }
}
