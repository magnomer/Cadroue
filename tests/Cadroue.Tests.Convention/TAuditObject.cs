using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditObject
{
    private static readonly string[] TAuditObjectVerdicts =
        ["Hydra", "Kraken", "Spider", "Chameleon", "Octopus", "Centipede", "Serpent", "Colony", "Hermit"];

    private static readonly Lazy<IReadOnlyList<TAuditObjectRow>> TAuditObjectRows = new(TAuditObjectRead);

    private static readonly Lazy<string> TAuditObjectWritten = new(TAuditReportSave);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditObject(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditObject_Types_HoldNoHydra()
    {
        TAuditObjectCheck("Hydra", "type(s) are one object behind many parts");
    }

    [Fact]
    public void AuditObject_Types_HoldNoKraken()
    {
        TAuditObjectCheck("Kraken", "type(s) are both too big and too coupled");
    }

    [Fact]
    public void AuditObject_Types_HoldNoSpider()
    {
        TAuditObjectCheck("Spider", "type(s) use many types and are used by many");
    }

    [Fact]
    public void AuditObject_Types_HoldNoChameleon()
    {
        TAuditObjectCheck("Chameleon", "type(s) hold too many settable slots");
    }

    [Fact]
    public void AuditObject_Types_HoldNoOctopus()
    {
        TAuditObjectCheck("Octopus", "type(s) use too many codebase types");
    }

    [Fact]
    public void AuditObject_Types_HoldNoCentipede()
    {
        TAuditObjectCheck("Centipede", "type(s) declare too many members");
    }

    [Fact]
    public void AuditObject_Types_HoldNoSerpent()
    {
        TAuditObjectCheck("Serpent", "type(s) run too many lines");
    }

    [Fact]
    public void AuditObject_Slots_HoldNoHub()
    {
        TAuditObjectCheck("Hub", "state slot(s) are reached from many parts");
    }

    [Fact]
    public void AuditObject_Parts_HoldsWithinCeiling()
    {
        List<string> over = TAuditOverRead();
        string report = TAuditObjectWritten.Value;
        _tAuditOutput.WriteLine(
            $"AUDITOBJECT Parts: {over.Count} type(s) above their part ceiling. Report: {report}");

        Assert.True(!TAuditObjectSetting.TAuditObjectEnforced || over.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{over.Count} type(s) are declared in more parts than their part ceiling. See {report}\n"
            + string.Join('\n', over.Select(row => "  " + row))));
    }

    [Fact]
    public void AuditObject_Ceiling_MatchesHits()
    {
        List<string> stale = TAuditStaleRead();
        string report = TAuditObjectWritten.Value;
        _tAuditOutput.WriteLine($"AUDITOBJECT Stale ceilings: {stale.Count}. Report: {report}");

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered. See {report}\n"
            + string.Join('\n', stale.Select(row => "  " + row))));
    }

    private static List<string> TAuditHitRead(string kind)
    {
        IReadOnlyList<TAuditObjectRow> rows = TAuditObjectRows.Value;
        if (kind == "Hub")
        {
            return rows
                .SelectMany(row => row.TAuditObjectHubs.Select(hub => $"{row.TAuditObjectName}: {hub}"))
                .ToList();
        }

        return rows
            .Where(row => row.TAuditObjectFlags.Contains(kind))
            .Select(row => $"{row.TAuditObjectName}: " + kind switch
            {
                "Hydra" => $"parts {row.TAuditObjectParts}, lines {row.TAuditObjectLines}, "
                    + $"fused {row.TAuditObjectFused:0.00}, density {row.TAuditObjectDensity:0.00}",
                "Kraken" => $"lines {row.TAuditObjectLines}, members {row.TAuditObjectMembers}, "
                    + $"outgoing {row.TAuditObjectOutgoing}, incoming {row.TAuditObjectIncoming}",
                "Spider" => $"outgoing {row.TAuditObjectOutgoing}, incoming {row.TAuditObjectIncoming}",
                "Chameleon" => $"mutable {row.TAuditObjectMutable}",
                "Octopus" => $"outgoing {row.TAuditObjectOutgoing}",
                "Centipede" => $"members {row.TAuditObjectMembers}",
                _ => $"lines {row.TAuditObjectLines}",
            })
            .ToList();
    }

    private static Dictionary<string, int> TAuditPartsRead()
    {
        return TAuditObjectRows.Value
            .Where(row => row.TAuditObjectParts > 1)
            .ToDictionary(row => row.TAuditObjectName, row => row.TAuditObjectParts, StringComparer.Ordinal);
    }

    private static List<string> TAuditOverRead()
    {
        return TAuditPartsRead()
            .Where(pair => pair.Value > TAuditObjectSetting.TAuditPartsCeiling.GetValueOrDefault(pair.Key, 1))
            .Select(pair => $"{pair.Key}: parts {pair.Value}, "
                + $"ceiling {TAuditObjectSetting.TAuditPartsCeiling.GetValueOrDefault(pair.Key, 1)}")
            .ToList();
    }

    private static List<string> TAuditAboveRead()
    {
        return TAuditObjectSetting.TAuditObjectCeiling
            .Where(pair => TAuditHitRead(pair.Key).Count > pair.Value)
            .Select(pair => $"{pair.Key}: {TAuditHitRead(pair.Key).Count} hit(s), ceiling {pair.Value}")
            .Concat(TAuditOverRead())
            .Where(_ => TAuditObjectSetting.TAuditObjectEnforced)
            .ToList();
    }

    private static List<string> TAuditStaleRead()
    {
        Dictionary<string, int> counts = TAuditPartsRead();
        foreach (string kind in TAuditObjectSetting.TAuditObjectCeiling.Keys)
        {
            counts[kind] = TAuditHitRead(kind).Count;
        }

        return TAuditObjectSetting.TAuditObjectCeiling
            .Concat(TAuditObjectSetting.TAuditPartsCeiling)
            .Where(pair => counts.GetValueOrDefault(pair.Key, 1) < pair.Value)
            .Select(pair => $"{pair.Key}: {counts.GetValueOrDefault(pair.Key, 1)} hit(s), ceiling {pair.Value}")
            .ToList();
    }

    private void TAuditObjectCheck(string kind, string summary)
    {
        List<string> hits = TAuditHitRead(kind);
        string report = TAuditObjectWritten.Value;
        int ceiling = TAuditObjectSetting.TAuditObjectCeiling.GetValueOrDefault(kind);
        _tAuditOutput.WriteLine($"AUDITOBJECT {kind}: {hits.Count} {summary}, ceiling {ceiling}. Report: {report}");

        bool held = !TAuditObjectSetting.TAuditObjectEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{hits.Count} {summary}, above the ceiling of {ceiling}. See {report}\n"
            + string.Join('\n', hits.Select(row => "  " + row))));
    }

    private static List<string> TAuditVerdictRead(IReadOnlyList<TAuditObjectRow> rows)
    {
        List<TAuditObjectRow> shown = rows
            .Where(row => row.TAuditObjectVerdict != "Hermit" || row.TAuditObjectShared > 0)
            .OrderBy(row => Array.IndexOf(TAuditObjectVerdicts, row.TAuditObjectVerdict))
            .ThenBy(row => row.TAuditObjectName, StringComparer.Ordinal)
            .ToList();
        List<string> text =
        [
            "",
            "## Verdicts",
            "",
            "| Type | Verdict | Flags | Hubs | Parts | Lines | Members | Mutable | Outgoing | Incoming |",
            "|---|---|---|---:|---:|---:|---:|---:|---:|---:|",
        ];
        text.AddRange(shown.Select(row => $"| `{row.TAuditObjectName}` | {row.TAuditObjectVerdict} "
            + $"| {(row.TAuditObjectFlags.Count > 0 ? string.Join(", ", row.TAuditObjectFlags) : "none")} "
            + $"| {row.TAuditObjectShared} | {row.TAuditObjectParts} | {row.TAuditObjectLines} "
            + $"| {row.TAuditObjectMembers} | {row.TAuditObjectMutable} | {row.TAuditObjectOutgoing} "
            + $"| {row.TAuditObjectIncoming} |"));
        text.Add("");
        text.Add($"Every other type ({rows.Count - shown.Count}) is a Hermit: one part, no flag and no hub.");
        return text;
    }

    private static IReadOnlyList<TAuditObjectRow> TAuditObjectRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditObjectSetting.TAuditObjectInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT", "No tracked source file was enumerated; the audit would pass vacuously."));
        Assert.True(TAuditBinder.TAuditWalkRead(sources).Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT", "No listed source file is bound, so the audit cannot judge."));
        return TAuditObjectWalker.TAuditRun(sources);
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        IReadOnlyList<TAuditObjectRow> rows = TAuditObjectRows.Value;
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditObjectSetting.TAuditObjectReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        List<TAuditObjectRow> split = rows.Where(row => row.TAuditObjectParts > 1).ToList();
        List<string> above = TAuditAboveRead();
        List<string> stale = TAuditStaleRead();
        IReadOnlyDictionary<string, double> hydra = TAuditObjectSetting.TAuditHydraLimit;
        List<string> text =
        [
            $"# Object audit {version}",
            "",
            $"- Generation: {TAuditConvention.TAuditGeneration}",
            $"- Enforced: {TAuditObjectSetting.TAuditObjectEnforced}",
            $"- Types: {rows.Count}, declared in several parts: {split.Count}",
            "- Verdicts: " + string.Join(", ", TAuditObjectVerdicts.Select(verdict =>
                $"{verdict} {rows.Count(row => row.TAuditObjectVerdict == verdict)}")),
        ];
        text.AddRange(TAuditObjectSetting.TAuditObjectCeiling
            .Select(pair => $"- {pair.Key}: {TAuditHitRead(pair.Key).Count}, ceiling {pair.Value}"));
        text.Add($"- Above ceiling: {above.Count}");
        text.Add($"- Stale ceilings: {stale.Count}");
        text.Add("");
        text.Add($"A Hydra has Parts {hydra["Parts"]} and Lines {hydra["Lines"]}, "
            + $"and Fused {hydra["Fused"]:0.00} or Density {hydra["Density"]:0.00}. "
            + "A Kraken is a Serpent or Centipede that is also an Octopus or Spider. "
            + $"A Spider has Outgoing {TAuditObjectSetting.TAuditSpiderLimit["Outgoing"]} "
            + $"and Incoming {TAuditObjectSetting.TAuditSpiderLimit["Incoming"]}. "
            + $"A Chameleon has Mutable {TAuditObjectSetting.TAuditChameleonLimit["Mutable"]}. "
            + $"An Octopus has Outgoing {TAuditObjectSetting.TAuditOctopusLimit["Outgoing"]}. "
            + $"A Centipede has Members {TAuditObjectSetting.TAuditCentipedeLimit["Members"]}. "
            + $"A Serpent has Lines {TAuditObjectSetting.TAuditSerpentLimit["Lines"]}. "
            + "Every value is reached at the limit or above, on the merged type. "
            + "A type's verdict is its worst flag, else Colony for several parts and Hermit for one. "
            + $"A hub is a state slot reached from {TAuditObjectSetting.TAuditHubLimit["Parts"]} or more parts. "
            + "It is a finding on the slot, never a verdict, so the verdict table shows the hub count beside it.");
        text.AddRange(TAuditVerdictRead(rows));
        text.Add("");
        text.Add("## Split types");
        text.Add("");
        text.Add("| Type | Parts | Lines | Members | Mutable | Outgoing | Incoming | Shared | Crossings "
            + "| Glued | Fused | Density | Verdict |");
        text.Add("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        text.AddRange(split.Select(row => $"| `{row.TAuditObjectName}` | {row.TAuditObjectParts} "
            + $"| {row.TAuditObjectLines} | {row.TAuditObjectMembers} | {row.TAuditObjectMutable} "
            + $"| {row.TAuditObjectOutgoing} | {row.TAuditObjectIncoming} | {row.TAuditObjectShared} "
            + $"| {row.TAuditObjectCrossings} | {row.TAuditObjectGlued:0.00} | {row.TAuditObjectFused:0.00} "
            + $"| {row.TAuditObjectDensity:0.00} | {row.TAuditObjectVerdict} |"));

        List<(string TAuditTitle, List<string> TAuditLines)> chapters = TAuditObjectSetting.TAuditObjectCeiling.Keys
            .Select(kind => (kind, TAuditHitRead(kind)))
            .Append(("Above ceiling", above))
            .Append(("Stale ceilings", stale))
            .ToList();
        foreach ((string title, List<string> lines) in chapters)
        {
            text.Add("");
            text.Add($"## {title}");
            if (lines.Count > 0)
            {
                text.Add("");
                text.AddRange(lines.Select(line => "- " + line));
            }
        }

        File.WriteAllText(path, string.Join('\n', text) + "\n", new UTF8Encoding(false));
        return Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
    }
}
