using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cadroue.UIDeportment;

// A prerendered icon's clear margin: a P32 or P256 bitmap is cropped to its visible pixels; any other bitmap, a fully
// clear one and one with no margin stay as they are.
internal static class QIconMargin
{
    public static BitmapSource QIconMarginResolve(Uri qIconUri, BitmapSource qIconBitmap)
    {
        if (!QIconPrerenderCheck(qIconUri.AbsolutePath))
        {
            return qIconBitmap;
        }

        if (QIconBoundsResolve(qIconBitmap) is not { } qIconBounds)
        {
            return qIconBitmap;
        }

        return QIconCropBuild(qIconBitmap, qIconBounds);
    }

    private static bool QIconPrerenderCheck(string qIconPath) =>
        qIconPath.Contains("/P32/", StringComparison.OrdinalIgnoreCase)
        || qIconPath.Contains("/P256/", StringComparison.OrdinalIgnoreCase);

    private static Int32Rect? QIconBoundsResolve(BitmapSource qIconBitmap)
    {
        int qIconPixelWidth = qIconBitmap.PixelWidth;
        int qIconPixelHeight = qIconBitmap.PixelHeight;
        var qIconPixels = new byte[qIconPixelWidth * qIconPixelHeight * 4];
        var qIconConverted = new FormatConvertedBitmap(qIconBitmap, PixelFormats.Bgra32, null, 0);
        qIconConverted.CopyPixels(qIconPixels, qIconPixelWidth * 4, 0);

        int qIconLeft = qIconPixelWidth;
        int qIconTop = qIconPixelHeight;
        int qIconRight = -1;
        int qIconBottom = -1;
        for (int qIconRow = 0; qIconRow < qIconPixelHeight; qIconRow++)
        {
            for (int qIconColumn = 0; qIconColumn < qIconPixelWidth; qIconColumn++)
            {
                if (qIconPixels[(qIconRow * qIconPixelWidth + qIconColumn) * 4 + 3] == 0)
                {
                    continue;
                }

                qIconLeft = Math.Min(qIconLeft, qIconColumn);
                qIconTop = Math.Min(qIconTop, qIconRow);
                qIconRight = Math.Max(qIconRight, qIconColumn);
                qIconBottom = Math.Max(qIconBottom, qIconRow);
            }
        }

        if (qIconRight < qIconLeft
            || (qIconRight - qIconLeft + 1) * (qIconBottom - qIconTop + 1) == qIconPixelWidth * qIconPixelHeight)
        {
            return null;
        }

        return new Int32Rect(qIconLeft, qIconTop, qIconRight - qIconLeft + 1, qIconBottom - qIconTop + 1);
    }

    private static BitmapSource QIconCropBuild(BitmapSource qIconBitmap, Int32Rect qIconBounds)
    {
        var qIconCropped = new CroppedBitmap(qIconBitmap, qIconBounds);
        qIconCropped.Freeze();
        return qIconCropped;
    }
}
