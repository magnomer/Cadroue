using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal readonly record struct PSDialogTheme(Brush PSDialogBandFill, int PSDialogDwmCaption)
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
        PSDialogApply(pWindow, pTheme.PSDialogBandFill, pTheme.PSDialogDwmCaption);

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

    private const string PSDialogPlacementKey = "PSDialog";

    internal static void PSDialogLocationAttach(Window pWindow)
    {
        PSDialogLocationRestore(pWindow);
        pWindow.Closed += (_, _) => PSDialogLocationSave(pWindow);
    }

    private static void PSDialogLocationRestore(Window pWindow)
    {
        Cadroue.Infrastructure.LPlacementRecord? pPlacement =
            Cadroue.Infrastructure.LPlacement.LPlacementRead(PSDialogPlacementKey);
        if (pPlacement is null)
        {
            return;
        }

        pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        pWindow.Left = pPlacement.LPlacementLeft;
        pWindow.Top = pPlacement.LPlacementTop;
    }

    private static void PSDialogLocationSave(Window pWindow)
    {
        Rect pBounds = pWindow.WindowState == WindowState.Normal
            ? new Rect(pWindow.Left, pWindow.Top, pWindow.ActualWidth, pWindow.ActualHeight)
            : pWindow.RestoreBounds;
        Cadroue.Infrastructure.LPlacement.LPlacementSave(
            PSDialogPlacementKey, pBounds.Left, pBounds.Top, pBounds.Width, pBounds.Height);
    }

    internal static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody) =>
        PSDialogBuild(pWindow, pTitle, pBody, PSHeadline.PSHeadlineBandBlue);

    internal static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody, PSDialogTheme pTheme) =>
        PSDialogBuild(pWindow, pTitle, pBody, pTheme.PSDialogBandFill);

    private static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody, Brush pBandFill)
    {
        pBody.Margin = new Thickness(0, PSHeadline.PSHeadlineBandHeight, 0, 0);
        var pRoot = new Grid { Background = pBandFill };
        pRoot.Children.Add(pBody);
        pRoot.Children.Add(PSHeadline.PSHeadlineBuild(pWindow, pTitle, pBandFill));
        return pRoot;
    }
}
