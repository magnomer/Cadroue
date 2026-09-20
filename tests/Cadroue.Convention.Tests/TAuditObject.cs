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
        List<string> hits = TAuditObjectRows.Value
            .Where(row => row.TAuditObjectMonolith)
            .Select(row => $"  {row.TAuditObjectName}: {row.TAuditObjectParts.Count} parts, "
                + $"{row.TAuditObjectLines} lines, {row.TAuditObjectCross} cross references, "
                + $"weave {row.TAuditObjectFree:0.00} without hubs, density {row.TAuditObjectDensity:0.00}")
            .ToList();
        TAuditObjectCheck("Monolith", hits, "split type(s) are one object behind many files");
    }

    [Fact]
    public void AuditObject_State_HoldNoHub()
    {
        List<string> hits = TAuditObjectRows.Value
            .SelectMany(row => row.TAuditObjectHubs.Select(hub => $"  {row.TAuditObjectName}: {hub}"))
            .ToList();
        TAuditObjectCheck("Hub", hits, "state slot(s) are reached from many parts");
    }

    [Fact]
    public void AuditObject_Parts_HoldWithinCeiling()
    {
        Dictionary<string, int> parts = TAuditPartRead();
        List<string> over = TAuditObjectSetting.TAuditPartCeiling
            .Where(pair => parts.GetValueOrDefault(pair.Key) > pair.Value)
            .Select(pair => $"  {pair.Key}: {parts.GetValueOrDefault(pair.Key)} part(s), ceiling {pair.Value}")
            .ToList();

        Assert.True(over.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{over.Count} type(s) are split over more parts than their ceiling.\n{string.Join('\n', over)}"));
    }

    [Fact]
    public void AuditObject_Ceiling_MatchesHits()
    {
        Dictionary<string, int> counts = new(StringComparer.Ordinal)
        {
            ["Monolith"] = TAuditObjectRows.Value.Count(row => row.TAuditObjectMonolith),
            ["Hub"] = TAuditObjectRows.Value.Sum(row => row.TAuditObjectHubs.Count),
        };
        foreach ((string name, int parts) in TAuditPartRead())
        {
            counts[name] = parts;
        }

        List<string> stale = TAuditObjectSetting.TAuditObjectCeiling
            .Concat(TAuditObjectSetting.TAuditPartCeiling)
            .Where(pair => counts.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"  {pair.Key}: {counts.GetValueOrDefault(pair.Key)} hit(s), ceiling {pair.Value}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    private static Dictionary<string, int> TAuditPartRead()
    {
        return TAuditObjectRows.Value
            .Where(row => TAuditObjectSetting.TAuditPartCeiling.ContainsKey(row.TAuditObjectName))
            .ToDictionary(row => row.TAuditObjectName, row => row.TAuditObjectParts.Count, StringComparer.Ordinal);
    }

    private void TAuditObjectCheck(string kind, List<string> hits, string summary)
    {
        string report = TAuditObjectWritten.Value;
        int ceiling = TAuditObjectSetting.TAuditObjectCeiling.GetValueOrDefault(kind);
        _tAuditOutput.WriteLine($"AUDITOBJECT {kind}: {hits.Count} {summary}, ceiling {ceiling}. Report: {report}");

        bool held = !TAuditObjectSetting.TAuditObjectEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITOBJECT",
            $"{hits.Count} {summary}, above the ceiling of {ceiling}. See {report}\n{string.Join('\n', hits)}"));
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
        StringBuilder text = new();
        text.AppendLine($"# Object audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditObjectSetting.TAuditObjectEnforced}");
        text.AppendLine($"- Types: {rows.Count}, split over several files: {split.Count}");
        text.AppendLine($"- Monolith: {rows.Count(row => row.TAuditObjectMonolith)}");
        text.AppendLine($"- Hub: {rows.Sum(row => row.TAuditObjectHubs.Count)}");
        text.AppendLine();
        text.AppendLine("A monolith has at least "
            + $"{TAuditObjectSetting.TAuditPartFloor} parts and {TAuditObjectSetting.TAuditLineFloor} lines, "
            + "and either "
            + $"its largest member component still spans {TAuditObjectSetting.TAuditWeaveFloor:0.00} of the parts once "
            + $"hub state is removed or it carries {TAuditObjectSetting.TAuditDensityFloor:0.00} cross references per "
            + $"member. A hub is a state slot reached from {TAuditObjectSetting.TAuditHubReach} or more parts.");
        text.AppendLine();
        text.AppendLine("## Split types");
        text.AppendLine();
        text.AppendLine(
            "| Type | Parts | Lines | Members | State | Hubs | Cross | Weave | Free | Density | Monolith |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        foreach (TAuditObjectRow row in split)
        {
            text.AppendLine($"| `{row.TAuditObjectName}` | {row.TAuditObjectParts.Count} | {row.TAuditObjectLines} "
                + $"| {row.TAuditObjectMembers} | {row.TAuditObjectState} | {row.TAuditObjectHubs.Count} "
                + $"| {row.TAuditObjectCross} | {row.TAuditObjectWeave:0.00} | {row.TAuditObjectFree:0.00} "
                + $"| {row.TAuditObjectDensity:0.00} | {(row.TAuditObjectMonolith ? "yes" : "")} |");
        }

        text.AppendLine();
        text.AppendLine("## Hubs");
        text.AppendLine();
        foreach (TAuditObjectRow row in rows.Where(row => row.TAuditObjectHubs.Count > 0))
        {
            foreach (string hub in row.TAuditObjectHubs)
            {
                text.AppendLine($"- `{row.TAuditObjectName}` {hub}");
            }
        }

        File.WriteAllText(path, text.ToString());
        return Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
    }
}
