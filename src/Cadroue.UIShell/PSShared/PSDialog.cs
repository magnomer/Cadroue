using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal readonly record struct PSDialogTheme(Brush BandFill, int DwmCaption)
{
    internal static readonly PSDialogTheme PSDialogThemeBlue =
        new(PSHeadline.PSHeadlineBandBlue, PSHeadline.PSHeadlineCaptionBlue);
    internal static readonly PSDialogTheme PSDialogThemeRed =
        new(PSHeadline.PSHeadlineBandRed, PSHeadline.PSHeadlineCaptionRed);
    internal static readonly PSDialogTheme PSDialogThemeOrange =
        new(PSHeadline.PSHeadlineBandOrange, PSHeadline.PSHeadlineCaptionOrange);
}

internal static class PSDialog
{
    internal static void PSDialogApply(Window pWindow, Brush pBackground) =>
        PSDialogApply(pWindow, pBackground, PSHeadline.PSHeadlineCaptionBlue);

    internal static void PSDialogApply(Window pWindow, PSDialogTheme pTheme) =>
        PSDialogApply(pWindow, pTheme.BandFill, pTheme.DwmCaption);

    private static void PSDialogApply(Window pWindow, Brush pBackground, int pCaption)
    {
        pWindow.WindowStyle = WindowStyle.None;
        pWindow.Background = pBackground;
        pWindow.FontSize = PSField.PSFieldFontSize;
        pWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        pWindow.UseLayoutRounding = true;
        pWindow.SnapsToDevicePixels = true;
        pWindow.SourceInitialized += (_, _) => PSCasement.PSCasementDwmApply(pWindow, pCaption);
    }

    internal static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody) =>
        PSDialogBuild(pWindow, pTitle, pBody, PSHeadline.PSHeadlineBandBlue);

    internal static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody, PSDialogTheme pTheme) =>
        PSDialogBuild(pWindow, pTitle, pBody, pTheme.BandFill);

    private static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody, Brush pBandFill)
    {
        pBody.Margin = new Thickness(0, PSHeadline.PSHeadlineBandHeight, 0, 0);
        var pRoot = new Grid { Background = pBandFill };
        pRoot.Children.Add(pBody);
        pRoot.Children.Add(PSHeadline.PSHeadlineBuild(pWindow, pTitle, pBandFill));
        return pRoot;
    }
}
