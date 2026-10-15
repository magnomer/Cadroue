using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Cadroue.UIDeportment;

internal static class QLook
{
    internal sealed record QLookSetter(
        string QLookSetterStyle,
        QLookCue QLookSetterCue,
        string QLookSetterPart,
        DependencyProperty QLookSetterProperty,
        string QLookSetterKey);

    internal static void QLookAttach(FrameworkElement lElement, string lStyle)
    {
        ArgumentNullException.ThrowIfNull(lElement);
        ArgumentException.ThrowIfNullOrEmpty(lStyle);

        IReadOnlyList<QLookSetter> lRows =
            [.. QLookSheet.QLookSheetState.Where(lRow => lRow.QLookSetterStyle == lStyle)];
        RoutedEventHandler lRouted = (_, _) => QLookStateApply(lElement, lRows);
        DependencyPropertyChangedEventHandler lChanged = (_, _) => QLookStateApply(lElement, lRows);

        lElement.MouseEnter += (_, _) => QLookStateApply(lElement, lRows);
        lElement.MouseLeave += (_, _) => QLookStateApply(lElement, lRows);
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
            lThumb.DragStarted += (_, _) => QLookStateApply(lElement, lRows);
            lThumb.DragCompleted += (_, _) => QLookStateApply(lElement, lRows);
        }

        _ = lElement.ApplyTemplate();
        QLookStateApply(lElement, lRows);
    }

    private static void QLookStateApply(FrameworkElement lElement, IReadOnlyList<QLookSetter> lRows)
    {
        QLookCue lActive = QLookCueRead(lElement.IsMouseOver, QLookCue.QLookCueHover)
            | QLookCueRead(lElement.IsKeyboardFocusWithin, QLookCue.QLookCueFocus)
            | QLookCueRead(lElement is ToggleButton { IsChecked: true }, QLookCue.QLookCueChecked)
            | QLookCueRead(lElement is ToggleButton { IsChecked: null }, QLookCue.QLookCueMixed)
            | QLookCueRead(!lElement.IsEnabled, QLookCue.QLookCueDisabled)
            | QLookCueRead(lElement is Thumb { IsDragging: true }, QLookCue.QLookCueDrag);

        Dictionary<(string, DependencyProperty), QLookSetter?> lWinners = [];
        foreach (QLookSetter lRow in lRows)
        {
            (string, DependencyProperty) lSlot = (lRow.QLookSetterPart, lRow.QLookSetterProperty);
            if ((lActive & lRow.QLookSetterCue) == lRow.QLookSetterCue)
            {
                lWinners[lSlot] = lRow;
            }
            else
            {
                lWinners.TryAdd(lSlot, null);
            }
        }

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

    private static QLookCue QLookCueRead(bool lShown, QLookCue lCue)
    {
        return lShown ? lCue : QLookCue.QLookCueBase;
    }
}
