using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TUsherPort : LUsherPort
{
    public List<string> TUsherCalls { get; } = [];

    public List<string?> TUsherFailures { get; } = [];

    public bool LUsherFileExist(string? lPath) => TUsherRecord("file", lPath) == "present";

    public bool LUsherFolderExist(string? lFolder) => TUsherRecord("folder", lFolder) == "present";

    public string LUsherNameRead(string lPath) => TUsherRecord("name", lPath) + ".name";

    public string LUsherStemRead(string lPath) => TUsherRecord("stem", lPath) + ".stem";

    public string LUsherProgramResolve(string lName) => TUsherRecord("program", lName) + ".program";

    public string? LUsherPathOpen(string lPath, string? lFallback, string? lFailure)
    {
        TUsherFailures.Add(lFailure);
        return TUsherRecord("reveal", $"{lPath}|{lFallback}") + ".failed";
    }

    public string? LUsherFolderOpen(string lFolder) =>
        TUsherRecord("explorer", lFolder) + ".failed";

    public string? LUsherLinkOpen(string lTarget, string? lFailure)
    {
        TUsherFailures.Add(lFailure);
        return TUsherRecord("link", lTarget) + ".failed";
    }

    private string? TUsherRecord(string lKind, string? lValue)
    {
        TUsherCalls.Add($"{lKind}:{lValue}");
        return lValue;
    }
}
