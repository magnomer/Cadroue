using System.Windows.Input;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public static class QShortcut
{
    public static string QShortcutGestureFormat(Key lKey, ModifierKeys lModifiers) =>
        QShortcutGestureFormat(
            QShortcutKey.QShortcutKeyFormat(lKey),
            lModifiers.HasFlag(ModifierKeys.Control),
            lModifiers.HasFlag(ModifierKeys.Alt),
            lModifiers.HasFlag(ModifierKeys.Shift),
            lModifiers.HasFlag(ModifierKeys.Windows));

    internal static string QShortcutGestureFormat(
        string lKeyName,
        bool lControl,
        bool lAlt,
        bool lShift,
        bool lWindows) =>
        LBinding.LBindingGestureFormat(lKeyName, lControl, lAlt, lShift, lWindows);

    internal static string? QShortcutTokenFind(string lGesture) =>
        LBinding.LBindingTokenFind(LBinding.LBindingCurrent, lGesture);
}
