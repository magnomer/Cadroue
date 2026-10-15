using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

public static class QSlider
{
    private const string QSliderStyle = "PSlider";

    private const string QSliderThumbStyle = "PSliderThumb";

    private const string QSliderThumbPart = "PSliderThumb";

    public static void QSliderAttach(Slider lSlider)
    {
        ArgumentNullException.ThrowIfNull(lSlider);

        lSlider.IsMoveToPointEnabled = true;
        lSlider.IsSnapToTickEnabled = false;
        lSlider.Style = QContract.QContractSheetFind<Style>(QSliderStyle);
        QLook.QLookAttach(lSlider, QSliderStyle);
        QLook.QLookAttach(QSliderThumbFind(lSlider), QSliderThumbStyle);
    }

    public static void QSliderResetAttach(Slider lSlider, Func<double> lDefaultRead)
    {
        ArgumentNullException.ThrowIfNull(lSlider);

        QSliderThumbFind(lSlider).PreviewMouseDoubleClick += (_, lEvent) =>
        {
            lSlider.Value = lDefaultRead();
            lEvent.Handled = true;
        };
    }

    private static Thumb QSliderThumbFind(Slider lSlider)
    {
        _ = lSlider.ApplyTemplate();
        return (Thumb)lSlider.Template.FindName(QSliderThumbPart, lSlider);
    }
}
