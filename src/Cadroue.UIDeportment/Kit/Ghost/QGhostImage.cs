using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Cadroue.UIDeportment;

internal static class QGhostImage
{
    private const string QGhostImageKey = "PGhostImage";

    internal static FrameworkElement QGhostImageBuild(FrameworkElement lSource)
    {
        DpiScale lDpi = VisualTreeHelper.GetDpi(lSource);
        QGhostSize lSize = QGhost.QGhostSizeResolve(
            lSource.ActualWidth,
            lSource.ActualHeight,
            lDpi.DpiScaleX,
            lDpi.DpiScaleY);
        var lBitmap = new RenderTargetBitmap(
            lSize.QGhostPixelWidth,
            lSize.QGhostPixelHeight,
            lSize.QGhostDpiX,
            lSize.QGhostDpiY,
            PixelFormats.Pbgra32);
        lBitmap.Render(lSource);
        lBitmap.Freeze();

        Rectangle lImage = QContract.QContractSheetFind<Rectangle>(QGhostImageKey);
        lImage.Width = lSource.ActualWidth;
        lImage.Height = lSource.ActualHeight;
        ((ImageBrush)lImage.Fill).ImageSource = lBitmap;
        return lImage;
    }
}
