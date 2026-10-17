using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal static class PSSubwindow
{
    internal static readonly Brush PSSubwindowBackground = PSSubwindowFillCreate();

    private const int PSSubwindowCaption = 0x00F7E8DC;

    internal static void PSSubwindowApply(Window pWindow)
    {
        PSSubwindowFrameApply(pWindow);
        QScrollbar.QScrollbarAttach(pWindow);
        PSSubwindowDwmAttach(pWindow);
    }

    private static void PSSubwindowFrameApply(Window pWindow)
    {
        pWindow.WindowStyle = WindowStyle.None;
        pWindow.ResizeMode = ResizeMode.NoResize;
        pWindow.Background = PSSubwindowBackground;
        pWindow.FontSize = PSField.PSFieldFontSize;
        pWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        pWindow.UseLayoutRounding = true;
        pWindow.SnapsToDevicePixels = true;
    }

    private static void PSSubwindowDwmAttach(Window pWindow) =>
        pWindow.SourceInitialized += (_, _) => PSCasement.PSCasementDwmApply(pWindow, PSSubwindowCaption);

    internal static Grid PSSubwindowBuild(Window pWindow, double pStripWidth, UIElement pSheetControl)
    {
        var pRoot = new Grid { Background = PSSubwindowBackground };
        pRoot.Children.Add(pSheetControl);
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(pWindow, pStripWidth));
        return pRoot;
    }

    private static Brush PSSubwindowFillCreate()
    {
        var pSubwindowBrush = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        pSubwindowBrush.Freeze();
        return pSubwindowBrush;
    }
}
