using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIShell.PSShared;

internal static class PSDialog
{
    internal static void PSDialogApply(Window pWindow, Brush pBackground)
    {
        pWindow.WindowStyle = WindowStyle.None;
        pWindow.Background = pBackground;
        pWindow.FontSize = PSField.PSFieldFontSize;
        pWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        pWindow.UseLayoutRounding = true;
        pWindow.SnapsToDevicePixels = true;
        pWindow.SourceInitialized += (_, _) => PSCasement.PSCasementDwmApply(pWindow);
    }

    internal static Grid PSDialogBuild(Window pWindow, string? pTitle, FrameworkElement pBody)
    {
        pBody.Margin = new Thickness(0, PSHeadline.PSHeadlineBandHeight, 0, 0);
        var pRoot = new Grid { Background = PSCasement.PSCasementBandFill };
        pRoot.Children.Add(pBody);
        pRoot.Children.Add(PSHeadline.PSHeadlineBuild(pWindow, pTitle));
        return pRoot;
    }
}
