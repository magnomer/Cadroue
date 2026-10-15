using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TPlacementPort : LPlacementPort
{
    public List<string> TPlacementCalls { get; } = [];

    public IEnumerable<(string, double, double, double, double)> LPlacementPreferenceRead(
        IReadOnlyList<(string, string)> lEntries)
    {
        TPlacementCalls.Add($"preference:{lEntries.Count}");
        return [("key", 1, 2, 3, 4)];
    }

    public void LPlacementUnreadableRecord(string lReason) => TPlacementCalls.Add($"unreadable:{lReason}");

    public void LPlacementBlockedRecord(string lKey) => TPlacementCalls.Add($"blocked:{lKey}");

    public void LPlacementUnsavedRecord(string lKey) => TPlacementCalls.Add($"unsaved:{lKey}");

    public void LPlacementCarriedRecord(string lKey) => TPlacementCalls.Add($"carried:{lKey}");

    public void LPlacementUncarriedRecord(Exception lFailure) =>
        TPlacementCalls.Add($"uncarried:{lFailure.Message}");
}
