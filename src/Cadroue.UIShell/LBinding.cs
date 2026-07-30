using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace Cadroue.UIShell;

public sealed class LBindingRecord
{
    public string LBindingRecordToken { get; set; } = string.Empty;

    public string LBindingRecordGesture { get; set; } = string.Empty;

    public LBindingRecord LBindingRecordClone() => new()
    {
        LBindingRecordToken = LBindingRecordToken,
        LBindingRecordGesture = LBindingRecordGesture
    };
}

public sealed record LBindingCommand(
    string LBindingCommandToken,
    string LBindingCommandKey,
    string LBindingCommandScope,
    string LBindingCommandGesture);

public static class LBinding
{
    public const string LBindingScopeGlobal = "Global";
    public const string LBindingScopeTab = "Tab";
    public const string LBindingScopeFlow = "Flow";
    public const string LBindingScopeSplit = "Split";

    private static readonly LBindingCommand[] lBindingCatalog =
    {
        new("Show", "Chrome.Shortcuts.Show", LBindingScopeGlobal, "Ctrl+/"),
        new("PlayPause", "Chrome.Shortcuts.PlayPause", LBindingScopeGlobal, "Space"),
        new("UnloadAll", "Chrome.Shortcuts.UnloadAll", LBindingScopeGlobal, "Shift+Delete"),
        new("Undo", "Chrome.Shortcuts.Undo", LBindingScopeTab, "Ctrl+Z"),
        new("Redo", "Chrome.Shortcuts.Redo", LBindingScopeTab, "Ctrl+Y"),
        new("Unload", "Chrome.Shortcuts.Unload", LBindingScopeTab, "F4"),
        new("ZoomIn", "Chrome.Shortcuts.ZoomIn", LBindingScopeFlow, "C"),
        new("ZoomOut", "Chrome.Shortcuts.ZoomOut", LBindingScopeFlow, "V"),
        new("KeyframePrevious", "Chrome.Shortcuts.KeyframePrevious", LBindingScopeFlow, "E"),
        new("KeyframeNearest", "Chrome.Shortcuts.KeyframeNearest", LBindingScopeFlow, "W"),
        new("KeyframeNext", "Chrome.Shortcuts.KeyframeNext", LBindingScopeFlow, "R"),
        new("SectionAdd", "Chrome.Shortcuts.SectionAdd", LBindingScopeSplit, "Q"),
        new("SectionStart", "Chrome.Shortcuts.SectionStart", LBindingScopeSplit, "D"),
        new("SectionSplit", "Chrome.Shortcuts.SectionSplit", LBindingScopeSplit, "S"),
        new("SectionEnd", "Chrome.Shortcuts.SectionEnd", LBindingScopeSplit, "F"),
        new("SectionDelete", "Chrome.Shortcuts.SectionDelete", LBindingScopeSplit, "Delete"),
        new("SectionRename", "Chrome.Shortcuts.SectionRename", LBindingScopeSplit, "A")
    };

    private static readonly string[] lBindingScopes =
    {
        LBindingScopeGlobal,
        LBindingScopeTab,
        LBindingScopeFlow,
        LBindingScopeSplit
    };

    public static IReadOnlyList<LBindingCommand> LBindingCatalogRead() => lBindingCatalog;

    public static IReadOnlyList<string> LBindingScopesRead() => lBindingScopes;

    public static IEnumerable<LBindingCommand> LBindingScopeRead(string lBindingScope) =>
        lBindingCatalog.Where(lBindingCommand =>
            string.Equals(lBindingCommand.LBindingCommandScope, lBindingScope, StringComparison.Ordinal));

    public static string LBindingLabelRead(string lBindingScope) => lBindingScope switch
    {
        LBindingScopeTab => "Chrome.Shortcuts.ScopeTab",
        LBindingScopeFlow => "Chrome.Shortcuts.ScopeFlow",
        LBindingScopeSplit => "Chrome.Shortcuts.ScopeSplit",
        _ => "Chrome.Shortcuts.ScopeGlobal"
    };

    public static List<LBindingRecord> LBindingDefaultCreate() =>
        lBindingCatalog
            .Select(lBindingCommand => new LBindingRecord
            {
                LBindingRecordToken = lBindingCommand.LBindingCommandToken,
                LBindingRecordGesture = lBindingCommand.LBindingCommandGesture
            })
            .ToList();

    public static string LBindingDefaultRead(string lBindingToken) =>
        lBindingCatalog
            .FirstOrDefault(lBindingCommand =>
                string.Equals(lBindingCommand.LBindingCommandToken, lBindingToken, StringComparison.Ordinal))
            ?.LBindingCommandGesture ?? string.Empty;

    public static List<LBindingRecord> LBindingNormalize(List<LBindingRecord>? lBindingRecords)
    {
        var lBindingResult = new List<LBindingRecord>();
        var lBindingTaken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (LBindingCommand lBindingCommand in lBindingCatalog)
        {
            LBindingRecord? lBindingStored = lBindingRecords?.FirstOrDefault(lBindingEntry =>
                string.Equals(lBindingEntry.LBindingRecordToken, lBindingCommand.LBindingCommandToken, StringComparison.Ordinal));

            string lBindingGesture = (lBindingStored?.LBindingRecordGesture
                ?? lBindingCommand.LBindingCommandGesture
                ?? string.Empty).Trim();

            if (lBindingGesture.Length > 0 && !lBindingTaken.Add(lBindingGesture))
            {
                lBindingGesture = string.Empty;
            }

            lBindingResult.Add(new LBindingRecord
            {
                LBindingRecordToken = lBindingCommand.LBindingCommandToken,
                LBindingRecordGesture = lBindingGesture
            });
        }

        return lBindingResult;
    }

    public static string LBindingGestureRead(List<LBindingRecord>? lBindingRecords, string lBindingToken) =>
        lBindingRecords?
            .FirstOrDefault(lBindingEntry =>
                string.Equals(lBindingEntry.LBindingRecordToken, lBindingToken, StringComparison.Ordinal))
            ?.LBindingRecordGesture ?? string.Empty;

    public static string? LBindingTokenFind(List<LBindingRecord>? lBindingRecords, string lBindingGesture)
    {
        if (string.IsNullOrEmpty(lBindingGesture) || lBindingRecords is null)
        {
            return null;
        }

        return lBindingRecords
            .FirstOrDefault(lBindingEntry =>
                string.Equals(lBindingEntry.LBindingRecordGesture, lBindingGesture, StringComparison.OrdinalIgnoreCase))
            ?.LBindingRecordToken;
    }

    public static string LBindingFormat(Key lBindingKey, ModifierKeys lBindingModifiers)
    {
        string lBindingName = LBindingKeyFormat(lBindingKey);
        if (lBindingName.Length == 0)
        {
            return string.Empty;
        }

        string lBindingPrefix = string.Empty;
        if ((lBindingModifiers & ModifierKeys.Control) != 0) lBindingPrefix += "Ctrl+";
        if ((lBindingModifiers & ModifierKeys.Alt) != 0) lBindingPrefix += "Alt+";
        if ((lBindingModifiers & ModifierKeys.Shift) != 0) lBindingPrefix += "Shift+";
        if ((lBindingModifiers & ModifierKeys.Windows) != 0) lBindingPrefix += "Win+";
        return lBindingPrefix + lBindingName;
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
