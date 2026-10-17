using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// Records every port call as one line, in call order; a run answers TMergeAnswer.
internal sealed class TMergePort : LMergePort
{
    public List<string> TMergeCalls { get; } = [];

    public int TMergeAnswer { get; set; }

    public int LMergeRun(
        string lPresetName,
        IReadOnlyList<(string LMergeName, IReadOnlyList<string> LMergePaths)> lGroups,
        IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab,
        IReadOnlyDictionary<string, Guid> lRelays)
    {
        TMergeCalls.Add(
            $"run {lPresetName}|"
            + string.Join(";", lGroups.Select(lGroup => $"{lGroup.LMergeName}={string.Join(",", lGroup.LMergePaths)}"))
            + $"|skip {TMergeSkipFormat(lSkipped)}|{(lUrgent ? "urgent" : "normal")}|{lRelayTarget}|{lSourceTab}|"
            + string.Join(
                ",", lRelays.OrderBy(lRelay => lRelay.Key).Select(lRelay => $"{lRelay.Key}@{lRelay.Value:N}")));
        return TMergeAnswer;
    }

    public void LMergeSkipRecord(IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped) =>
        TMergeCalls.Add($"skip {TMergeSkipFormat(lSkipped)}");

    public void LMergeHeldRecord(Guid lSourceTab) => TMergeCalls.Add($"held {lSourceTab}");

    private static string TMergeSkipFormat(
        IReadOnlyList<(string LMergeName, int LMergeLocked, int LMergeCount)> lSkipped) =>
        string.Join(",", lSkipped.Select(lSkip => $"{lSkip.LMergeName}:{lSkip.LMergeLocked}/{lSkip.LMergeCount}"));
}
