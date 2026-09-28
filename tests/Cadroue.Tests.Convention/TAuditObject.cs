using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditObject
{
    private static readonly Lazy<IReadOnlyList<TAuditObjectRow>> TAuditObjectRows = new(TAuditObjectRead);

    private static readonly Lazy<string> TAuditObjectWritten = new(TAuditReportSave);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditObject(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditObject_SplitTypes_HoldNoMonolith()
    {
        TAuditObjectCheck("Monolith", TAuditHitRead("Monolith"), "split type(s) are one object behind many parts");
    }

    [Fact]
    public void AuditObject_SingleTypes_HoldNoLarge()
    {
        TAuditObjectCheck("Large", TAuditHitRead("Large"), "single-part type(s) are too large for one object");
    }

    [Fact]
    public void AuditObject_State_HoldNoHub()
    {
        TAuditObjectCheck("Hub", TAuditHitRead("Hub"), "state slot(s) are reached from many parts");
    }

    [Fact]
    public void AuditObject_Parts_HoldWithinCeiling()
    {
        List<string> over = TAuditOverRead();
        string report = TAuditObjectWritten.Value;
        _tAuditOutput.WriteLine($"AUDITOBJECT Parts: {over.Count} type(s) above their part ceiling. Report: {report}");

        Assert.True(!TAuditObjectSetting.TAuditObjectEnforced || over.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{over.Count} type(s) are split over more parts than their ceiling. See {report}\n"
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
        return kind switch
        {
            "Monolith" => rows
                .Where(row => row.TAuditObjectMonolith)
                .Select(row => $"{row.TAuditObjectName}: {row.TAuditObjectParts.Count} parts, "
                    + $"{row.TAuditObjectLines} lines, {row.TAuditObjectCross} cross references, "
                    + $"weave {row.TAuditObjectFree:0.00} without hubs, density {row.TAuditObjectDensity:0.00}")
                .ToList(),
            "Hub" => rows
                .SelectMany(row => row.TAuditObjectHubs.Select(hub => $"{row.TAuditObjectName}: {hub}"))
                .ToList(),
            _ => rows
                .Where(row => row.TAuditObjectLarge)
                .Select(row => $"{row.TAuditObjectName}: {row.TAuditObjectLines} lines, "
                    + $"{row.TAuditObjectMembers} members, {row.TAuditObjectState} state slots")
                .ToList(),
        };
    }

    private static Dictionary<string, int> TAuditPartRead()
    {
        return TAuditObjectRows.Value
            .Where(row => row.TAuditObjectParts.Count > 1)
            .ToDictionary(row => row.TAuditObjectName, row => row.TAuditObjectParts.Count, StringComparer.Ordinal);
    }

    private static List<string> TAuditOverRead()
    {
        return TAuditPartRead()
            .Where(pair => pair.Value > TAuditObjectSetting.TAuditPartCeiling.GetValueOrDefault(pair.Key, 1))
            .Select(pair => $"{pair.Key}: {pair.Value} part(s), "
                + $"ceiling {TAuditObjectSetting.TAuditPartCeiling.GetValueOrDefault(pair.Key, 1)}")
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
        Dictionary<string, int> counts = TAuditPartRead();
        foreach (string kind in TAuditObjectSetting.TAuditObjectCeiling.Keys)
        {
            counts[kind] = TAuditHitRead(kind).Count;
        }

        return TAuditObjectSetting.TAuditObjectCeiling
            .Concat(TAuditObjectSetting.TAuditPartCeiling)
            .Where(pair => counts.GetValueOrDefault(pair.Key, 1) < pair.Value)
            .Select(pair => $"{pair.Key}: {counts.GetValueOrDefault(pair.Key, 1)} hit(s), ceiling {pair.Value}")
            .ToList();
    }

    private void TAuditObjectCheck(string kind, List<string> hits, string summary)
    {
        string report = TAuditObjectWritten.Value;
        int ceiling = TAuditObjectSetting.TAuditObjectCeiling.GetValueOrDefault(kind);
        _tAuditOutput.WriteLine($"AUDITOBJECT {kind}: {hits.Count} {summary}, ceiling {ceiling}. Report: {report}");

        bool held = !TAuditObjectSetting.TAuditObjectEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{hits.Count} {summary}, above the ceiling of {ceiling}. See {report}\n"
            + string.Join('\n', hits.Select(row => "  " + row))));
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
        return TAuditObjectWalker.TAuditRun(sources, repoRoot);
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        IReadOnlyList<TAuditObjectRow> rows = TAuditObjectRows.Value;
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditObjectSetting.TAuditObjectReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        List<TAuditObjectRow> split = rows.Where(row => row.TAuditObjectParts.Count > 1).ToList();
        List<string> above = TAuditAboveRead();
        List<string> stale = TAuditStaleRead();
        List<string> text =
        [
            $"# Object audit {version}",
            "",
            $"- Generation: {TAuditConvention.TAuditGeneration}",
            $"- Enforced: {TAuditObjectSetting.TAuditObjectEnforced}",
            $"- Types: {rows.Count}, split into several parts: {split.Count}",
        ];
        text.AddRange(TAuditObjectSetting.TAuditObjectCeiling
            .Select(pair => $"- {pair.Key}: {TAuditHitRead(pair.Key).Count}, ceiling {pair.Value}"));
        text.Add($"- Above ceiling: {above.Count}");
        text.Add($"- Stale ceilings: {stale.Count}");
        text.Add("");
        text.Add("A monolith has at least "
            + $"{TAuditObjectSetting.TAuditPartLimit} parts and {TAuditObjectSetting.TAuditSpanLimit} lines, "
            + "and either "
            + $"its largest member component still spans {TAuditObjectSetting.TAuditWeaveLimit:0.00} of the parts once "
            + $"hub state is removed or it carries {TAuditObjectSetting.TAuditDensityLimit:0.00} cross references per "
            + $"member. A hub is a state slot reached from {TAuditObjectSetting.TAuditHubReach} or more parts. "
            + $"A large type has one part and at least {TAuditObjectSetting.TAuditLargeLines} lines, "
            + $"{TAuditObjectSetting.TAuditLargeMembers} members "
            + $"or {TAuditObjectSetting.TAuditLargeState} state slots. "
            + "A part is one declaration of the type, so one file may hold several parts.");
        text.Add("");
        text.Add("## Split types");
        text.Add("");
        text.Add("| Type | Parts | Lines | Members | State | Hubs | Cross | Weave | Free | Density | Monolith |");
        text.Add("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        text.AddRange(split.Select(row => $"| `{row.TAuditObjectName}` | {row.TAuditObjectParts.Count} "
            + $"| {row.TAuditObjectLines} | {row.TAuditObjectMembers} | {row.TAuditObjectState} "
            + $"| {row.TAuditObjectHubs.Count} | {row.TAuditObjectCross} | {row.TAuditObjectWeave:0.00} "
            + $"| {row.TAuditObjectFree:0.00} | {row.TAuditObjectDensity:0.00} "
            + $"| {(row.TAuditObjectMonolith ? "yes" : "no")} |"));

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
