using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TEditPort : LEditPort
{
    public List<string> TEditCalls { get; } = [];

    public void LEditAllRun(
        string lPresetName,
        IReadOnlyList<(string LEditPath, Guid LEditBatch)> lSources,
        Guid lRelayTarget,
        Guid lSourceTab) =>
        TEditCalls.Add(
            $"all:{lPresetName}|"
            + string.Join(",", lSources.Select(lSource => $"{lSource.LEditPath}@{lSource.LEditBatch:N}"))
            + $"|{lRelayTarget}|{lSourceTab}");
}
