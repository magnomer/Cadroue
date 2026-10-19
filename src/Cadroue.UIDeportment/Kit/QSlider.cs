using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

public static class QSlider
{
    private const string QSliderStyle = "PSlider";

    private const string QSliderThumbPart = "PSliderThumb";

    // Reads the value a double-click on the thumb restores; a slider without one ignores the double-click.
    internal static readonly DependencyProperty QSliderOriginProperty = DependencyProperty.RegisterAttached(
        "QSliderOrigin",
        typeof(Func<double>),
        typeof(QSlider),
        new FrameworkPropertyMetadata(null));

    internal static void QSliderHook() =>
        EventManager.RegisterClassHandler(
            typeof(Slider),
            FrameworkElement.LoadedEvent,
            new RoutedEventHandler((lSender, _) => QSliderLoad((Slider)lSender)),
            true);

    public static void QSliderAttach(Slider lSlider)
    {
        ArgumentNullException.ThrowIfNull(lSlider);

        lSlider.Style = QContract.QContractSheetFind<Style>(QSliderStyle);
        QSliderLoad(lSlider);
        QLook.QLookAttach(lSlider);
    }

    public static void QSliderResetAttach(Slider lSlider, Func<double> lDefaultRead)
    {
        ArgumentNullException.ThrowIfNull(lSlider);

        lSlider.SetValue(QSliderOriginProperty, lDefaultRead);
    }

    private static void QSliderLoad(Slider lSlider)
    {
        if (QLook.QLookStyleResolve(lSlider, [QSliderStyle]) is null)
        {
            return;
        }

        QSliderPointerSet(lSlider);
        Thumb lThumb = QSliderThumbFind(lSlider);
        QLook.QLookAttach(lThumb);
        lThumb.PreviewMouseDoubleClick += (_, lEvent) => QSliderResetHandle(lSlider, lEvent);
    }

    private static void QSliderPointerSet(Slider lSlider)
    {
        lSlider.IsMoveToPointEnabled = true;
        lSlider.IsSnapToTickEnabled = false;
    }

    private static void QSliderResetHandle(Slider lSlider, MouseButtonEventArgs lEvent)
    {
        if (lSlider.GetValue(QSliderOriginProperty) is not Func<double> lDefaultRead)
        {
            return;
        }

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
