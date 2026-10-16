using System.Windows;

namespace Cadroue.UIDeportment;

// A window's bounds, read from the window and applied back to it.
public static class QSashWindow
{
    public static QSashBounds QSashWindowRead(Window lWindow) =>
        new(lWindow.Left, lWindow.Top, lWindow.ActualWidth, lWindow.ActualHeight);

    public static void QSashWindowApply(Window lWindow, QSashBounds lBounds)
    {
        lWindow.Left = lBounds.QSashLeft;
        lWindow.Top = lBounds.QSashTop;
        lWindow.Width = lBounds.QSashWidth;
        lWindow.Height = lBounds.QSashHeight;
    }
}
