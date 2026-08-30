using System.Windows;

namespace Cadroue.UIShell.PSShared;

internal static class PSHeadline
{
    internal const double PSHeadlineBandHeight = 42;
    internal const double PSHeadlineTitleFont = 12;

    internal static UIElement PSHeadlineBuild(Window pWindow, string? pTitle) =>
        PSCasement.PSCasementOverlayBuild(
            pWindow, 0, pTitle, pCloseOnly: true, PSHeadlineBandHeight, PSHeadlineTitleFont);
}
