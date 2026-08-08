using System.Windows.Input;

namespace Cadroue.UIShell;

public static class LBinding
{
    public static string LBindingFormat(Key lBindingKey, ModifierKeys lBindingModifiers)
    {
        string lBindingName = LBindingKeyFormat(lBindingKey);
        return Cadroue.Infrastructure.LBinding.LBindingGestureFormat(
            lBindingName,
            (lBindingModifiers & ModifierKeys.Control) != 0,
            (lBindingModifiers & ModifierKeys.Alt) != 0,
            (lBindingModifiers & ModifierKeys.Shift) != 0,
            (lBindingModifiers & ModifierKeys.Windows) != 0);
    }

    public static bool LBindingModifierCheck(Key lBindingKey) => lBindingKey is
        Key.LeftCtrl or Key.RightCtrl
        or Key.LeftAlt or Key.RightAlt
        or Key.LeftShift or Key.RightShift
        or Key.LWin or Key.RWin
        or Key.System or Key.None or Key.ImeProcessed;

    private static string LBindingKeyFormat(Key lBindingKey)
    {
        if (LBindingModifierCheck(lBindingKey) || lBindingKey is Key.Enter or Key.Escape)
        {
            return string.Empty;
        }

        if (lBindingKey is >= Key.A and <= Key.Z)
        {
            return lBindingKey.ToString();
        }

        if (lBindingKey is >= Key.D0 and <= Key.D9)
        {
            return ((int)(lBindingKey - Key.D0)).ToString();
        }

        if (lBindingKey is >= Key.F1 and <= Key.F24)
        {
            return "F" + (int)(lBindingKey - Key.F1 + 1);
        }

        if (lBindingKey is >= Key.NumPad0 and <= Key.NumPad9)
        {
            return "Num" + (int)(lBindingKey - Key.NumPad0);
        }

        return lBindingKey switch
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
