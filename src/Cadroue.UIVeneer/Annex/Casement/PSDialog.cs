using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

internal static class PSDialog
{
    internal static void PSDialogApply(Window pWindow, Brush pBackground) =>
        PSDialogApply(pWindow, pBackground, PSHeadline.PSHeadlineCaptionBlue);

    internal static void PSDialogApply(Window pWindow, PSDialogTheme pTheme) =>
        PSDialogApply(pWindow, pTheme.PSDialogBandFill, pTheme.PSDialogDwmCaption);

    private static void PSDialogApply(Window pWindow, Brush pBackground, int pCaption)
    {
        PSDialogFrameApply(pWindow, pBackground);
        PSDialogDwmAttach(pWindow, pCaption);
    }

    private static void PSDialogFrameApply(Window pWindow, Brush pBackground)
    {
        pWindow.WindowStyle = WindowStyle.None;
        pWindow.Background = pBackground;
        pWindow.FontSize = PSField.PSFieldFontSize;
        pWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        pWindow.UseLayoutRounding = true;
        pWindow.SnapsToDevicePixels = true;
    }

    private static void PSDialogDwmAttach(Window pWindow, int pCaption) =>
        pWindow.SourceInitialized += (_, _) => PSCasementDwm.PSCasementDwmApply(pWindow, pCaption);

    internal static void PSDialogOwnerApply(Window pWindow, Window? pOwner)
    {
        Window? pResolvedOwner = PSDialogOwnerResolve(pOwner);
        if (pResolvedOwner is not null && pResolvedOwner != pWindow)
        {
            pWindow.Owner = pResolvedOwner;
        }
        else
        {
            pWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private static Window? PSDialogOwnerResolve(Window? pOwner) =>
        pOwner ?? System.Windows.Application.Current?.MainWindow;

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
