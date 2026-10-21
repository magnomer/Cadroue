using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The white balance colour wheel: draws the wheel from the gate's pixels, hands each press to the gate and places
// the dot where the gate puts it on a wheel of the section's size.
public sealed class QWhitebalanceWheel
{
    public const int QWhitebalanceWheelSize = 120;

    private const double QWhitebalanceWheelOrigin = 0.9;
    private const string QWhitebalanceWheelSheet = "PWhitebalanceWheel";
    private const string QWhitebalanceWheelCanvas = "PWhitebalanceWheelCanvas";
    private const string QWhitebalanceWheelImage = "PWhitebalanceWheelImage";
    private const string QWhitebalanceWheelDot = "PWhitebalanceWheelDot";
    private const string QWhitebalanceWheelBrightness = "PWhitebalanceWheelBrightness";

    private readonly CWhitebalance qWhitebalanceGate;
    private readonly Canvas qWhitebalanceWheelCanvas;
    private readonly Image qWhitebalanceWheelImage;
    private readonly Ellipse qWhitebalanceDot;
    private readonly Slider qWhitebalanceWheelBrightness;

    public QWhitebalanceWheel(QInspector lInspector)
    {
        qWhitebalanceGate = lInspector.QInspectorVideo.QInspectorWhitebalance;
        QWhitebalanceWheelBody = QContract.QContractSheetFind<StackPanel>(QWhitebalanceWheelSheet);
        qWhitebalanceWheelCanvas = QContract.QContractFind<Canvas>(
            QWhitebalanceWheelBody, QWhitebalanceWheelCanvas);
        qWhitebalanceWheelImage = QContract.QContractFind<Image>(
            QWhitebalanceWheelBody, QWhitebalanceWheelImage);
        qWhitebalanceDot = QContract.QContractFind<Ellipse>(QWhitebalanceWheelBody, QWhitebalanceWheelDot);
        qWhitebalanceWheelBrightness = QContract.QContractFind<Slider>(
            QWhitebalanceWheelBody, QWhitebalanceWheelBrightness);
        qWhitebalanceWheelBrightness.Value = QWhitebalanceWheelOrigin;
        QSlider.QSliderAttach(qWhitebalanceWheelBrightness);
    }

    public StackPanel QWhitebalanceWheelBody { get; }

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

    public void QWhitebalanceWheelAttach()
    {
        qWhitebalanceWheelCanvas.MouseLeftButtonDown += (_, lMouse) =>
        {
            qWhitebalanceWheelCanvas.CaptureMouse();
            QWhitebalanceWheelHandle(lMouse);
        };
        qWhitebalanceWheelCanvas.MouseMove += (_, lMouse) => QWhitebalanceWheelHandle(lMouse);
        qWhitebalanceWheelCanvas.MouseLeftButtonUp += (_, _) => qWhitebalanceWheelCanvas.ReleaseMouseCapture();
        qWhitebalanceWheelBrightness.ValueChanged += (_, _) => QWhitebalanceWheelDraw();
        QWhitebalanceWheelDraw();
    }

    private void QWhitebalanceWheelDraw() =>
        qWhitebalanceWheelImage.Source =
            QWhitebalanceWheelDraw(QWhitebalanceWheelSize, qWhitebalanceWheelBrightness.Value);

    private void QWhitebalanceWheelHandle(MouseEventArgs lMouse)
    {
        Point lPoint = lMouse.GetPosition(qWhitebalanceWheelCanvas);
        QWhitebalanceWheelHandle(
            lMouse.LeftButton == MouseButtonState.Pressed,
            lPoint.X,
            lPoint.Y,
            QWhitebalanceWheelSize);
    }

    internal void QWhitebalanceWheelPlace()
    {
        (double lLeft, double lTop, bool lPresent) =
            qWhitebalanceGate.CWhitebalanceDotRead(QWhitebalanceWheelSize, qWhitebalanceDot.Width);
        Canvas.SetLeft(qWhitebalanceDot, lLeft);
        Canvas.SetTop(qWhitebalanceDot, lTop);
        qWhitebalanceDot.Visibility = lPresent ? Visibility.Visible : Visibility.Collapsed;
    }
}
