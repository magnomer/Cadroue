using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

internal static class QLook
{
    internal const string QLookPopupPart = "PART_Popup";

    internal static void QLookAttach(FrameworkElement lElement, string lStyle)
    {
        ArgumentNullException.ThrowIfNull(lElement);
        ArgumentException.ThrowIfNullOrEmpty(lStyle);

        RoutedEventHandler lRouted = (_, _) => QLookStateApply(lElement, lStyle);
        DependencyPropertyChangedEventHandler lChanged = (_, _) => QLookStateApply(lElement, lStyle);

        lElement.MouseEnter += (_, _) => QLookStateApply(lElement, lStyle);
        lElement.MouseLeave += (_, _) => QLookStateApply(lElement, lStyle);
        lElement.IsKeyboardFocusWithinChanged += lChanged;
        lElement.IsEnabledChanged += lChanged;
        if (lElement is ToggleButton lToggle)
        {
            lToggle.Checked += lRouted;
            lToggle.Unchecked += lRouted;
            lToggle.Indeterminate += lRouted;
        }

        if (lElement is Thumb lThumb)
        {
            lThumb.DragStarted += (_, _) => QLookStateApply(lElement, lStyle);
            lThumb.DragCompleted += (_, _) => QLookStateApply(lElement, lStyle);
        }

        if (lElement is ComboBox lCombo)
        {
            lCombo.DropDownOpened += (_, _) => QLookStateApply(lElement, lStyle);
            lCombo.DropDownClosed += (_, _) => QLookStateApply(lElement, lStyle);
        }

        if (lElement is ListBoxItem lItem)
        {
            lItem.Selected += lRouted;
            lItem.Unselected += lRouted;
        }

        if (lElement is ButtonBase)
        {
            lElement.AddHandler(UIElement.MouseDownEvent, lRouted, true);
            lElement.AddHandler(UIElement.MouseUpEvent, lRouted, true);
            lElement.AddHandler(UIElement.MouseMoveEvent, lRouted, true);
            lElement.AddHandler(UIElement.LostMouseCaptureEvent, lRouted, true);
            lElement.AddHandler(UIElement.KeyDownEvent, lRouted, true);
            lElement.AddHandler(UIElement.KeyUpEvent, lRouted, true);
        }
        else
        {
            lElement.PreviewMouseLeftButtonDown += lRouted.Invoke;
            lElement.PreviewMouseLeftButtonUp += lRouted.Invoke;
        }

        _ = lElement.ApplyTemplate();
        if ((lElement as Control)?.Template?.FindName(QLookPopupPart, lElement) is Popup lPopup)
        {
            lPopup.Opened += (_, _) => QLookStateApply(lElement, lStyle);
            lPopup.Closed += (_, _) => QLookStateApply(lElement, lStyle);
        }

        QLookStateApply(lElement, lStyle);
    }

    private static void QLookStateApply(FrameworkElement lElement, string lStyle)
    {
        IReadOnlyDictionary<(string, DependencyProperty), QLookSetter?> lWinners =
            QLookSheet.QLookSheetResolve(lStyle, QLookFact.QLookFactRead(lElement));
        foreach (((string lPart, DependencyProperty lProperty), QLookSetter? lWinner) in lWinners)
        {
            if ((lElement as Control)?.Template?.FindName(lPart, lElement) is not FrameworkElement lTarget)
            {
                continue;
            }

            if (lWinner is null)
            {
                lTarget.ClearValue(lProperty);
            }
            else
            {
                lTarget.SetResourceReference(lProperty, lWinner.QLookSetterKey);
            }
        }
    }
}
