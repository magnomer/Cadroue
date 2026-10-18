using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal static class PSFader
{
    internal const double PSFaderWidth = 220;

    internal static UIElement PSFaderRowBuild(Slider pSlider, TextBox pReadout)
    {
        pReadout.Width = 88;
        pReadout.Margin = new Thickness(12, 0, 0, 0);
        pReadout.VerticalAlignment = VerticalAlignment.Center;
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = PSField.PSFieldControlHeight
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pReadout);
        return pRow;
    }

    internal static Slider PSFaderCreate(double pMinimum, double pMaximum, double pValue)
    {
        double pTop = Math.Max(pMaximum, pMinimum);
        return PSFaderStepBuild(pMinimum, pTop, 1, Math.Clamp(pValue, pMinimum, pTop));
    }

    internal static Slider PSFaderStepBuild(double pMinimum, double pMaximum, double pStep, double pValue)
    {
        var pSlider = new Slider
        {
            Minimum = pMinimum,
            Maximum = pMaximum,
            SmallChange = pStep,
            LargeChange = pStep,
            TickFrequency = pStep,
            IsSnapToTickEnabled = true,
            Value = pValue,
            Width = PSFaderWidth,
            VerticalAlignment = VerticalAlignment.Center
        };
        QSlider.QSliderAttach(pSlider);
        return pSlider;
    }

    internal static void PSFaderReadoutSet(TextBox pReadout, string pText) => pReadout.Text = pText;

    internal static void PSFaderPairSet(Slider pSlider, TextBox pReadout, double pPosition, string pText)
    {
        pSlider.Value = pPosition;
        pReadout.Text = pText;
        pReadout.CaretIndex = pReadout.Text.Length;
    }

    internal static void PSFaderSyncSet(StrongBox<bool> pSync, bool pActive) => pSync.Value = pActive;

    internal static void PSFaderEditApply(TextBox pReadout, bool pReadOnly) => pReadout.IsReadOnly = pReadOnly;

    internal static Visibility PSFaderNoticeResolve(double pAt) =>
        pAt <= 0 ? Visibility.Visible : Visibility.Collapsed;

    internal static void PSFaderNoticeSet(UIElement? pNotice, Visibility pVisibility)
    {
        if (pNotice is not null)
        {
            pNotice.Visibility = pVisibility;
        }
    }

    internal static void PSFaderReturnHandle(KeyEventArgs pEvent, Action pCommit)
    {
        if (pEvent.Key == Key.Return)
        {
            pCommit();
            pEvent.Handled = true;
        }
    }

    internal static UIElement PSFaderRowBuild(Slider pSlider, FrameworkElement pTrailing)
    {
        pTrailing.Margin = new Thickness(12, 0, 0, 0);
        pTrailing.VerticalAlignment = VerticalAlignment.Center;
        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            MinHeight = PSField.PSFieldControlHeight
        };
        pRow.Children.Add(pSlider);
        pRow.Children.Add(pTrailing);
        return pRow;
    }
}
