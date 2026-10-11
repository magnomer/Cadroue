using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TUsherPort : LUsherPort
{
    public List<string> TUsherCalls { get; } = [];

    public bool LUsherFileExist(string? lPath) => TUsherRecord("file", lPath) == "present";

    public bool LUsherFolderExist(string? lFolder) => TUsherRecord("folder", lFolder) == "present";

    public string LUsherNameRead(string lPath) => TUsherRecord("name", lPath) + ".name";

    public string LUsherStemRead(string lPath) => TUsherRecord("stem", lPath) + ".stem";

    public string LUsherExtensionRead(string lPath) => TUsherRecord("extension", lPath) + ".extension";

    public string LUsherProgramResolve(string lName) => TUsherRecord("program", lName) + ".program";

    public string LUsherPathResolve(string lPath) => TUsherRecord("path", lPath) + ".path";

    public string? LUsherPathOpen(string lPath, string lLabel, string? lFallback) =>
        TUsherRecord("reveal", $"{lPath}|{lLabel}|{lFallback}") + ".failed";

    public string? LUsherFolderOpen(string lFolder, string lLabel) =>
        TUsherRecord("explorer", $"{lFolder}|{lLabel}") + ".failed";

    public string? LUsherLinkOpen(string lTarget, string lLabel) =>
        TUsherRecord("link", $"{lTarget}|{lLabel}") + ".failed";

    private string? TUsherRecord(string lKind, string? lValue)
    {
        TUsherCalls.Add($"{lKind}:{lValue}");
        return lValue;
    }
}
