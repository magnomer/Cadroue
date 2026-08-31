using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.UIShell.PMainWindow;

namespace Cadroue.UIShell.PSShared;

internal static class PSSubwindow
{
    internal static readonly Brush PSSubwindowBackground = PSSubwindowFillCreate();

    // Deliberately duplicated, not shared with the tabless Dialog path: caption (DWM 0x00BBGGRR)
    // is PSSubwindowBackground in BGR form, and both are meant to match PSHeadline's blue window.
    private const int PSSubwindowCaption = 0x00F7E8DC;

    internal static void PSSubwindowApply(Window pWindow)
    {
        pWindow.WindowStyle = WindowStyle.None;
        pWindow.ResizeMode = ResizeMode.NoResize;
        pWindow.Background = PSSubwindowBackground;
        pWindow.FontSize = PSField.PSFieldFontSize;
        pWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        pWindow.UseLayoutRounding = true;
        pWindow.SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(pWindow);
        pWindow.SourceInitialized += (_, _) => PSCasement.PSCasementDwmApply(pWindow, PSSubwindowCaption);
    }

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
