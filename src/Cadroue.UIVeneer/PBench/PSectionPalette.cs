using System.Windows.Media;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PBench;

internal static class PSectionPalette
{
    private const byte PSectionBandAlpha = 0x99;
    private const byte PSectionBadgeAlpha = 0xFF;

    internal static Brush PSectionBandRead(int pColorIndex) =>
        PSectionBrushBuild(LSection.LSectionHexRead(pColorIndex), PSectionBandAlpha);

    internal static Brush PSectionBadgeRead(int pColorIndex) =>
        PSectionBrushBuild(LSection.LSectionHexRead(pColorIndex), PSectionBadgeAlpha);

    internal static IReadOnlyList<Brush> PSectionBadgesRead(string pName) =>
        LSection.LSectionPaletteRead(pName).Select(PSectionBadgeBuild).ToList();

    private static Brush PSectionBadgeBuild(string pHex) => PSectionBrushBuild(pHex, PSectionBadgeAlpha);

    private static Brush PSectionBrushBuild(string pHex, byte pAlpha)
    {
        var pColor = (Color)ColorConverter.ConvertFromString(pHex);
        var pBrush = new SolidColorBrush(Color.FromArgb(pAlpha, pColor.R, pColor.G, pColor.B));
        pBrush.Freeze();
        return pBrush;
    }
}
