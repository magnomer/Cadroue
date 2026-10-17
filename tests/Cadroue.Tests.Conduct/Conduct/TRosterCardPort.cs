using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TRosterCardPort : LRosterCardPort
{
    public List<LRosterRow> TRosterCardRows { get; } = [];

    public List<string> TRosterCardCalls { get; } = [];

    public IReadOnlyList<LRosterRow> LRosterCardRead(IReadOnlyList<Guid> lIds, Guid lRunner)
    {
        TRosterCardCalls.Add($"{lRunner}:{string.Join(",", lIds)}");
        return lIds
            .SelectMany(lId => TRosterCardRows.Where(lRow => lRow.LRosterRowId == lId).Take(1))
            .ToArray();
    }
}
