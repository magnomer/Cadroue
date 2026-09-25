using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditFake
{
    private static readonly Lazy<IReadOnlyList<TViolation>> TAuditFakeRows = new(TAuditFakeRead);

    private static readonly Lazy<string> TAuditFakeWritten = new(TAuditReportSave);

    private static readonly Regex TAuditMarkupWord = new("[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditFake(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditFake_Members_HoldNoOrphan()
    {
        TAuditFakeCheck("Orphan", "member(s) are read by nothing live");
    }

    [Fact]
    public void AuditFake_Members_HoldNoTested()
    {
        TAuditFakeCheck("Tested", "member(s) are read only by tests");
    }

    [Fact]
    public void AuditFake_Ceiling_MatchesHits()
    {
        List<string> stale = TAuditFakeSetting.TAuditFakeCeiling
            .Where(pair => TAuditTallyRead(pair.Key) < pair.Value)
            .Select(pair => $"  {pair.Key}: {TAuditTallyRead(pair.Key)} hit(s), ceiling {pair.Value}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITFAKE",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    private static int TAuditTallyRead(string kind)
    {
        return TAuditFakeRows.Value.Count(row => row.TViolationKind == kind);
    }

    private void TAuditFakeCheck(string kind, string summary)
    {
        List<string> hits = TAuditFakeRows.Value
            .Where(row => row.TViolationKind == kind)
            .Select(row => $"  {row.TViolationPath}:{row.TViolationLine} {row.TViolationName}: {row.TViolationReason}")
            .ToList();
        string report = TAuditFakeWritten.Value;
        int ceiling = TAuditFakeSetting.TAuditFakeCeiling.GetValueOrDefault(kind);
        _tAuditOutput.WriteLine($"AUDITFAKE {kind}: {hits.Count} {summary}, ceiling {ceiling}. Report: {report}");

        bool held = !TAuditFakeSetting.TAuditFakeEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITFAKE",
            $"{hits.Count} {summary}, above the ceiling of {ceiling}. See {report}\n{string.Join('\n', hits)}"));
    }

    private static IReadOnlyList<TViolation> TAuditFakeRead()
    {
        string repoRoot = TAuditBinder.TAuditRoot;
        IReadOnlyList<string> tests = TAuditSource.TAuditFileRead(repoRoot, new TAuditScope(
            [],
            TAuditFakeSetting.TAuditFakeInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []));
        IReadOnlyList<string> markup = TAuditSource.TAuditFileRead(repoRoot, new TAuditScope(
            [],
            TAuditFakeSetting.TAuditMarkupInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            [],
            [],
            []));
        Assert.True(tests.Count > 0 && markup.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITFAKE", "No tracked test or markup file was enumerated; the audit would pass vacuously."));

        HashSet<string> words = markup
            .SelectMany(path => TAuditMarkupWord.Matches(File.ReadAllText(path)).Select(match => match.Value))
            .ToHashSet(StringComparer.Ordinal);
        return TAuditFakeWalker.TAuditRun(tests, words);
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditBinder.TAuditRoot;
        IReadOnlyList<TViolation> rows = TAuditFakeRows.Value;
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditFakeSetting.TAuditFakeReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        StringBuilder text = new();
        text.AppendLine($"# Fake audit {version}");
        text.AppendLine();
        text.AppendLine($"- Generation: {TAuditConvention.TAuditGeneration}");
        text.AppendLine($"- Enforced: {TAuditFakeSetting.TAuditFakeEnforced}");
        foreach (string kind in TAuditFakeSetting.TAuditFakeCeiling.Keys)
        {
            text.AppendLine($"- {kind}: {TAuditTallyRead(kind)}");
        }

        text.AppendLine();
        text.AppendLine("A member is live when a live reader, a constructor, an override, generated code, markup or "
            + "the serializer reads it. Orphan is read by nothing live. Tested is read only by tests.");
        foreach (string kind in TAuditFakeSetting.TAuditFakeCeiling.Keys)
        {
            text.AppendLine();
            text.AppendLine($"## {kind}");
            text.AppendLine();
            foreach (TViolation row in rows.Where(row => row.TViolationKind == kind))
            {
                string place = $"{row.TViolationPath}:{row.TViolationLine}";
                text.AppendLine($"- `{place}` `{row.TViolationName}`: {row.TViolationReason}");
            }
        }

        File.WriteAllText(path, text.ToString());
        return Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
    }
}
