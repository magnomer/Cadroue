using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TWorkspacePort : LWorkspacePort
{
    public List<string> TWorkspaceCalls { get; } = [];

    public (bool LWorkspaceLoadCancelled, bool LWorkspaceMediaClosed) LWorkspaceRemovalRead(
        IReadOnlyCollection<string> lRemoved, string? lIntent, string? lSource)
    {
        TWorkspaceCalls.Add($"removal:{string.Join(",", lRemoved)}|{lIntent}|{lSource}");
        return (true, false);
    }

    public (bool LWorkspaceMediaClosed, bool LWorkspaceLoadCancelled, bool LWorkspaceMediaCleared) LWorkspaceClearRead(
        IReadOnlyCollection<string> lProtected, string? lSource, string? lIntent, bool lLoaded)
    {
        TWorkspaceCalls.Add($"clear:{string.Join(",", lProtected)}|{lSource}|{lIntent}|{lLoaded}");
        return (false, true, true);
    }

    public string LWorkspacePathResolve(string lPath) => "full:" + lPath;
}
