namespace Cadroue.Infrastructure;

public sealed record LBindingCommand(
    string LBindingCommandToken,
    string LBindingCommandKey,
    string LBindingCommandScope,
    string LBindingCommandGesture);

public static class LBindingCatalog
{
    public const string LBindingScopeGlobal = "Global";
    public const string LBindingScopeTab = "Tab";
    public const string LBindingScopeFlow = "Flow";
    public const string LBindingScopeSplit = "Split";

    private static readonly LBindingCommand[] lBindingCatalogCommands =
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

    public static IReadOnlyList<LBindingCommand> LBindingCatalogRead() => lBindingCatalogCommands;

    public static IEnumerable<LBindingCommand> LBindingScopeRead(string lBindingScope) =>
        lBindingCatalogCommands.Where(lBindingCommand =>
            string.Equals(lBindingCommand.LBindingCommandScope, lBindingScope, StringComparison.Ordinal));

    public static string LBindingLabelRead(string lBindingScope) => lBindingScope switch
    {
        LBindingScopeTab => "Chrome.Shortcuts.ScopeTab",
        LBindingScopeFlow => "Chrome.Shortcuts.ScopeFlow",
        LBindingScopeSplit => "Chrome.Shortcuts.ScopeSplit",
        _ => "Chrome.Shortcuts.ScopeGlobal"
    };

    public static string LBindingDefaultRead(string lBindingToken) =>
        lBindingCatalogCommands
            .FirstOrDefault(lBindingCommand =>
                string.Equals(lBindingCommand.LBindingCommandToken, lBindingToken, StringComparison.Ordinal))
            ?.LBindingCommandGesture ?? string.Empty;
}
