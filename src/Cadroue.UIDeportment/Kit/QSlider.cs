using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

public static class QSlider
{
    private const string QSliderStyle = "PSlider";

    private const string QSliderThumbStyle = "PSliderThumb";

    private const string QSliderThumbPart = "PSliderThumb";

    public static void QSliderAttach(Slider lSlider)
    {
        ArgumentNullException.ThrowIfNull(lSlider);

        QSliderPointerSet(lSlider);
        QSliderLookAttach(lSlider);
    }

    public static void QSliderResetAttach(Slider lSlider, Func<double> lDefaultRead)
    {
        ArgumentNullException.ThrowIfNull(lSlider);

        QSliderThumbFind(lSlider).PreviewMouseDoubleClick += (_, lEvent) =>
            QSliderResetHandle(lSlider, lDefaultRead, lEvent);
    }

    private static void QSliderPointerSet(Slider lSlider)
    {
        lSlider.IsMoveToPointEnabled = true;
        lSlider.IsSnapToTickEnabled = false;
    }

    private static void QSliderLookAttach(Slider lSlider)
    {
        lSlider.Style = QContract.QContractSheetFind<Style>(QSliderStyle);
        QLook.QLookAttach(lSlider, QSliderStyle);
        QLook.QLookAttach(QSliderThumbFind(lSlider), QSliderThumbStyle);
    }

    private static void QSliderResetHandle(Slider lSlider, Func<double> lDefaultRead, MouseButtonEventArgs lEvent)
    {
        QSliderValueReset(lSlider, lDefaultRead);
        lEvent.Handled = true;
    }

    private static void QSliderValueReset(Slider lSlider, Func<double> lDefaultRead) =>
        lSlider.Value = lDefaultRead();

    private static Thumb QSliderThumbFind(Slider lSlider)
    {
        _ = lSlider.ApplyTemplate();
        return (Thumb)lSlider.Template.FindName(QSliderThumbPart, lSlider);
    }
}
