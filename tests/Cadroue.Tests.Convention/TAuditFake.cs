using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
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
        List<string> stale = TAuditStaleRead();
        string report = TAuditFakeWritten.Value;
        _tAuditOutput.WriteLine($"AUDITFAKE Stale ceilings: {stale.Count}. Report: {report}");

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITFAKE",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n"
            + string.Join('\n', stale.Select(row => "  " + row))));
    }

    private static int TAuditTallyRead(string kind)
    {
        return TAuditFakeRows.Value.Count(row => row.TViolationKind == kind);
    }

    private static List<string> TAuditStaleRead()
    {
        return TAuditFakeSetting.TAuditFakeCeiling
            .Where(pair => TAuditTallyRead(pair.Key) < pair.Value)
            .Select(pair => $"{pair.Key}: {TAuditTallyRead(pair.Key)} hit(s), ceiling {pair.Value}")
            .ToList();
    }

    private static List<string> TAuditAboveRead()
    {
        return TAuditFakeSetting.TAuditFakeCeiling
            .Where(pair => TAuditFakeSetting.TAuditFakeEnforced && TAuditTallyRead(pair.Key) > pair.Value)
            .Select(pair => $"{pair.Key}: {TAuditTallyRead(pair.Key)} hit(s), ceiling {pair.Value}")
            .ToList();
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
            TAuditNameSetting.TAuditExcludedSuffixes,
            TAuditNameSetting.TAuditExcludedPrefixes,
            []));
        Assert.True(tests.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITFAKE", "No tracked test file was enumerated; the audit would pass vacuously."));

        List<XElement> nodes = markup.SelectMany(path => XDocument.Load(path).Descendants()).ToList();
        HashSet<string> words = nodes
            .SelectMany(node => node.Attributes().Where(attribute => !attribute.IsNamespaceDeclaration))
            .SelectMany(attribute => TAuditMarkupWord.Matches(attribute.Value).Select(match => match.Value)
                .Append(attribute.Name.LocalName))
            .Concat(nodes.Select(node => node.Name.LocalName[(node.Name.LocalName.LastIndexOf('.') + 1)..]))
            .ToHashSet(StringComparer.Ordinal);
        HashSet<string> elements = nodes
            .Select(node => node.Name.LocalName)
            .Where(name => !name.Contains('.', StringComparison.Ordinal))
            .ToHashSet(StringComparer.Ordinal);
        return TAuditFakeWalker.TAuditRun(tests, words, elements);
    }

    private static string TAuditReportSave()
    {
        string repoRoot = TAuditBinder.TAuditRoot;
        IReadOnlyList<TViolation> rows = TAuditFakeRows.Value;
        string version = TAuditSource.TAuditVersionRead(repoRoot);
        string path = Path.Combine(repoRoot, string.Format(TAuditFakeSetting.TAuditFakeReport, version));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        List<string> above = TAuditAboveRead();
        List<string> stale = TAuditStaleRead();
        List<string> text =
        [
            $"# Fake audit {version}",
            "",
            $"- Generation: {TAuditConvention.TAuditGeneration}",
            $"- Enforced: {TAuditFakeSetting.TAuditFakeEnforced}",
        ];
        text.AddRange(TAuditFakeSetting.TAuditFakeCeiling
            .Select(pair => $"- {pair.Key}: {TAuditTallyRead(pair.Key)}, ceiling {pair.Value}"));
        text.Add($"- Above ceiling: {above.Count}");
        text.Add($"- Stale ceilings: {stale.Count}");
        text.Add("");
        text.Add("A member is live when a live reader, a constructor, an override, generated code, markup or "
            + "the serializer reads it. Orphan is read by nothing live. Tested is read only by tests.");
        List<(string TAuditTitle, List<string> TAuditLines)> chapters = TAuditFakeSetting.TAuditFakeCeiling.Keys
            .Select(kind => (kind, rows.Where(row => row.TViolationKind == kind)
                .Select(row => $"- `{row.TViolationPath}:{row.TViolationLine}` `{row.TViolationName}`: "
                    + row.TViolationReason)
                .ToList()))
            .Append(("Above ceiling", above.Select(row => "- " + row).ToList()))
            .Append(("Stale ceilings", stale.Select(row => "- " + row).ToList()))
            .ToList();
        foreach ((string title, List<string> lines) in chapters)
        {
            text.Add("");
            text.Add($"## {title}");
            if (lines.Count > 0)
            {
                text.Add("");
                text.AddRange(lines);
            }
        }

        File.WriteAllText(path, string.Join('\n', text) + "\n", new UTF8Encoding(false));
        return Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
    }
}
