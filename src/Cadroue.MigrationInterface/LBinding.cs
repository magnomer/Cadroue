using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.MigrationInterface;

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

    public static List<LBindingRecord> LBindingCurrent { get; private set; } = LBindingNormalize(null);

    public static void LBindingLoad() =>
        LBindingCurrent = LBindingNormalize(LBindingStore.LBindingLoad());

    public static void LBindingSet(List<LBindingRecord> lBindingRecords)
    {
        List<LBindingRecord> lBindingPrevious = LBindingCurrent;
        LBindingCurrent = LBindingNormalize(lBindingRecords);
        LBindingStore.LBindingSave(LBindingCurrent);

        int lBindingChanged = LBindingCurrent.Count(lBindingNext =>
            !string.Equals(
                LBindingGestureRead(lBindingPrevious, lBindingNext.LBindingRecordToken),
                lBindingNext.LBindingRecordGesture,
                StringComparison.Ordinal));
        if (lBindingChanged > 0)
        {
            LTraceLog.LTraceInfoRecord($"Shortcuts: {lBindingChanged} binding(s) changed");
        }
    }

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
}
