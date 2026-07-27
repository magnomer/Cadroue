using System.Windows.Media;

namespace Cadroue.UIShell.PFlow;

internal static class PSectionPalette
{
    private static readonly Color[] pSectionColors =
    {
        Color.FromRgb(0x4A, 0x90, 0xD9),
        Color.FromRgb(0x27, 0xAE, 0x60),
        Color.FromRgb(0xE6, 0x7E, 0x22),
        Color.FromRgb(0x8E, 0x44, 0xAD),
        Color.FromRgb(0xE7, 0x4C, 0x3C),
        Color.FromRgb(0x16, 0xA0, 0x85),
    };

    private const byte PSectionBandAlpha = 0x99;

    private static readonly Brush[] pSectionBandBrushes = PSectionBrushesCreate(PSectionBandAlpha);
    private static readonly Brush[] pSectionBadgeBrushes = PSectionBrushesCreate(0xFF);

    private static Brush[] PSectionBrushesCreate(byte pSectionAlpha)
    {
        var pSectionBrushes = new Brush[pSectionColors.Length];
        for (int pSectionIndex = 0; pSectionIndex < pSectionColors.Length; pSectionIndex++)
        {
            Color pSectionColor = pSectionColors[pSectionIndex];
            var pSectionBrush = new SolidColorBrush(
                Color.FromArgb(pSectionAlpha, pSectionColor.R, pSectionColor.G, pSectionColor.B));
            pSectionBrush.Freeze();
            pSectionBrushes[pSectionIndex] = pSectionBrush;
        }

        return pSectionBrushes;
    }

    internal static int PSectionPaletteCount => pSectionColors.Length;

    internal static Brush PSectionPaletteRead(int pColorIndex)
        => pSectionBandBrushes[Math.Abs(pColorIndex) % pSectionBandBrushes.Length];

    internal static Brush PSectionBadgeRead(int pColorIndex)
        => pSectionBadgeBrushes[Math.Abs(pColorIndex) % pSectionBadgeBrushes.Length];
}
