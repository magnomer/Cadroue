using System.Windows;
using System.Windows.Controls.Primitives;

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
            | QLookFactSelect(lElement is Thumb { IsDragging: true }, QLookCue.QLookCueDrag);
    }

    private static QLookCue QLookFactSelect(bool lShown, QLookCue lCue)
    {
        return lShown ? lCue : QLookCue.QLookCueBase;
    }
}
