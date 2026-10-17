using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIVeneer;

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
        pWindow.SourceInitialized += (_, _) => PSCasement.PSCasementDwmApply(pWindow, pCaption);

    private const string PSDialogPlacementKey = "PSDialog";

    internal static void PSDialogLocationAttach(Window pWindow)
    {
        PSDialogLocationRestore(pWindow);
        PSDialogClosedAttach(pWindow);
    }

    private static void PSDialogClosedAttach(Window pWindow) =>
        pWindow.Closed += (_, _) => PSDialogLocationSave(pWindow);

    private static void PSDialogLocationRestore(Window pWindow)
    {
        if (!Cadroue.UIDeportment.QPlacement.QPlacementRead(
                PSDialogPlacementKey, out double pLeft, out double pTop, out _, out _))
        {
            return;
        }

        PSDialogLocationPlace(pWindow, pLeft, pTop);
    }

    private static void PSDialogLocationPlace(Window pWindow, double pLeft, double pTop)
    {
        pWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        pWindow.Left = pLeft;
        pWindow.Top = pTop;
    }

    private static void PSDialogLocationSave(Window pWindow)
    {
        Rect pBounds = PSDialogBoundsResolve(pWindow);
        Cadroue.UIDeportment.QPlacement.QPlacementSave(
            PSDialogPlacementKey, pBounds.Left, pBounds.Top, pBounds.Width, pBounds.Height);
    }

    private static Rect PSDialogBoundsResolve(Window pWindow) =>
        pWindow.WindowState == WindowState.Normal
            ? new Rect(pWindow.Left, pWindow.Top, pWindow.ActualWidth, pWindow.ActualHeight)
            : pWindow.RestoreBounds;

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
