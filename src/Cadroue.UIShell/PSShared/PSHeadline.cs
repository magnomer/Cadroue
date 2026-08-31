using System.Windows;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal static class PSHeadline
{
    internal const double PSHeadlineBandHeight = 42;
    internal const double PSHeadlineTitleFont = 12;

    // Kept separate from PSSubwindow's caption on purpose (tabless vs tabbed); the two blue
    // captions are meant to match — change both together.
    internal const int PSHeadlineCaptionBlue = 0x00F7E8DC;
    internal const int PSHeadlineCaptionRed = 0x00EAEAFC;
    internal const int PSHeadlineCaptionOrange = 0x00EAF2FC;

    internal static readonly Brush PSHeadlineBandBlue = PSHeadlineFillCreate(0xEA, 0xF2, 0xFC);
    internal static readonly Brush PSHeadlineBandRed = PSHeadlineFillCreate(0xFC, 0xEA, 0xEA);
    internal static readonly Brush PSHeadlineBandOrange = PSHeadlineFillCreate(0xFC, 0xF2, 0xEA);

    internal static UIElement PSHeadlineBuild(Window pWindow, string? pTitle) =>
        PSHeadlineBuild(pWindow, pTitle, PSHeadlineBandBlue);

    internal static UIElement PSHeadlineBuild(Window pWindow, string? pTitle, Brush pBandFill) =>
        PSCasement.PSCasementOverlayBuild(
            pWindow, 0, pTitle, pCloseOnly: true, PSHeadlineBandHeight, PSHeadlineTitleFont, pBandFill);

    private static Brush PSHeadlineFillCreate(byte pRed, byte pGreen, byte pBlue)
    {
        var pHeadlineBrush = new SolidColorBrush(Color.FromRgb(pRed, pGreen, pBlue));
        pHeadlineBrush.Freeze();
        return pHeadlineBrush;
    }
}
