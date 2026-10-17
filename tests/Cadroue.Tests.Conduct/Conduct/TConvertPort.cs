using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TConvertPort : LConvertPort
{
    public List<string> TConvertCalls { get; } = [];

    public void LConvertRun(
        string lPresetName,
        IReadOnlyList<(string LConvertPath, Guid LConvertBatch)> lSources,
        bool lUrgent,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        TConvertCalls.Add(
            $"{lPresetName}|"
            + string.Join(",", lSources.Select(lSource => $"{lSource.LConvertPath}@{lSource.LConvertBatch:N}"))
            + $"|{(lUrgent ? "urgent" : "normal")}|{lRelayTarget}|{lSourceTab}");
}
