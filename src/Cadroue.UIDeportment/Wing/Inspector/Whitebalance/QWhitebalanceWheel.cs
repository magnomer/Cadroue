using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The white balance colour wheel: draws the wheel from the gate's pixels, hands each press to the gate and places
// the dot where the gate puts it on a wheel of the section's size.
public sealed class QWhitebalanceWheel
{
    private readonly CWhitebalance qWhitebalanceGate;
    private readonly Ellipse qWhitebalanceDot;
    private readonly int qWhitebalanceSize;

    public QWhitebalanceWheel(CWhitebalance lGate, Ellipse lDot, int lSize)
    {
        qWhitebalanceGate = lGate;
        qWhitebalanceDot = lDot;
        qWhitebalanceSize = lSize;
    }

    // The colour wheel at the given size and brightness.
    public ImageSource QWhitebalanceWheelDraw(int lSize, double lValue)
    {
        (int lSide, int lStride, byte[] lPixels) = qWhitebalanceGate.CWhitebalanceBitmapRead(lSize, lValue);
        var lBitmap = new WriteableBitmap(lSide, lSide, 96, 96, PixelFormats.Bgra32, null);
        lBitmap.WritePixels(new Int32Rect(0, 0, lSide, lSide), lPixels, lStride, 0);
        lBitmap.Freeze();
        return lBitmap;
    }

    public void QWhitebalanceWheelHandle(bool lPressed, double lX, double lY, double lSize) =>
        qWhitebalanceGate.CWhitebalanceWheelHandle(lPressed, lX, lY, lSize);

    internal void QWhitebalanceWheelPlace()
    {
        (double lLeft, double lTop, bool lPresent) =
            qWhitebalanceGate.CWhitebalanceDotRead(qWhitebalanceSize, qWhitebalanceDot.Width);
        Canvas.SetLeft(qWhitebalanceDot, lLeft);
        Canvas.SetTop(qWhitebalanceDot, lTop);
        qWhitebalanceDot.Visibility = lPresent ? Visibility.Visible : Visibility.Collapsed;
    }
}
