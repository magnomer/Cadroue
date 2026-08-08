using System.Windows.Input;

namespace Cadroue.UIShell.PMainWindow;

public static class PShortcut
{
    public static string PShortcutGestureFormat(Key pShortcutKey, ModifierKeys pShortcutModifiers)
    {
        string pShortcutName = PShortcutKeyFormat(pShortcutKey);
        return Cadroue.Infrastructure.LBinding.LBindingGestureFormat(
            pShortcutName,
            (pShortcutModifiers & ModifierKeys.Control) != 0,
            (pShortcutModifiers & ModifierKeys.Alt) != 0,
            (pShortcutModifiers & ModifierKeys.Shift) != 0,
            (pShortcutModifiers & ModifierKeys.Windows) != 0);
    }

    public static bool PShortcutModifierCheck(Key pShortcutKey) => pShortcutKey is
        Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin
        or Key.System or Key.None or Key.ImeProcessed;

    private static string PShortcutKeyFormat(Key pShortcutKey)
    {
        if (PShortcutModifierCheck(pShortcutKey) || pShortcutKey is Key.Enter or Key.Escape)
        {
            return string.Empty;
        }

        if (pShortcutKey is >= Key.A and <= Key.Z)
        {
            return pShortcutKey.ToString();
        }

        if (pShortcutKey is >= Key.D0 and <= Key.D9)
        {
            return ((int)(pShortcutKey - Key.D0)).ToString();
        }

        if (pShortcutKey is >= Key.F1 and <= Key.F24)
        {
            return "F" + (int)(pShortcutKey - Key.F1 + 1);
        }

        if (pShortcutKey is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return "Num" + (int)(pShortcutKey - Key.NumPad0);
        }

        return pShortcutKey switch
        {
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.OemQuestion or Key.Divide => "/",
            Key.OemComma => ",",
            Key.OemPeriod => ".",
            Key.OemMinus or Key.Subtract => "-",
            Key.OemPlus or Key.Add => "+",
            Key.Multiply => "Num*",
            Key.Decimal => "Num.",
            Key.OemTilde => "`",
            Key.OemOpenBrackets => "[",
            Key.OemCloseBrackets => "]",
            Key.OemPipe or Key.OemBackslash => "\\",
            Key.OemSemicolon => ";",
            Key.OemQuotes => "'",
            _ => string.Empty
        };
    }
}
