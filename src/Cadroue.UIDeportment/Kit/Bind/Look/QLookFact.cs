using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Cadroue.UIDeportment;

internal static class QLookFact
{
    internal static QLookCue QLookFactRead(FrameworkElement lElement)
    {
        return QLookFactSelect(lElement.IsMouseOver, QLookCue.QLookCueHover)
            | QLookFactSelect(lElement.IsKeyboardFocusWithin, QLookCue.QLookCueFocus)
            | QLookFactSelect(lElement is ToggleButton { IsChecked: true }, QLookCue.QLookCueChecked)
            | QLookFactSelect(lElement is ToggleButton { IsChecked: null }, QLookCue.QLookCueMixed)
            | QLookFactSelect(!lElement.IsEnabled, QLookCue.QLookCueDisabled)
            | QLookFactSelect(lElement is Thumb { IsDragging: true }, QLookCue.QLookCueDrag)
            | QLookFactSelect(lElement is ComboBox { IsDropDownOpen: true }, QLookCue.QLookCueExpanded)
            | QLookFactSelect(
                (lElement as Control)?.Template?.FindName(QLook.QLookPopupPart, lElement) is Popup { IsOpen: true },
                QLookCue.QLookCueExpanded)
            | QLookFactSelect(lElement is ListBoxItem { IsSelected: true }, QLookCue.QLookCueSelected)
            | QLookFactSelect(lElement is ButtonBase { IsPressed: true }, QLookCue.QLookCuePressed)
            | QLookFactSelect(
                lElement is not ButtonBase && lElement.IsMouseOver && Mouse.LeftButton == MouseButtonState.Pressed,
                QLookCue.QLookCuePressed);
    }

    private static QLookCue QLookFactSelect(bool lShown, QLookCue lCue)
    {
        return lShown ? lCue : QLookCue.QLookCueBase;
    }
}
