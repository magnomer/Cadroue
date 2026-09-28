using Xunit;
using Xunit.Abstractions;

namespace Convention.Tests;

public sealed class TAuditLine
{
    private static readonly Lazy<IReadOnlyList<TAuditLineRow>> TAuditLineRows = new(TAuditLineRead);

    private readonly ITestOutputHelper _tAuditOutput;

    public TAuditLine(ITestOutputHelper output)
    {
        _tAuditOutput = output;
    }

    [Fact]
    public void AuditLine_File_HoldWithinLength()
    {
        List<string> hits = TAuditLineRows.Value
            .Where(row => row.TAuditLineCount > TAuditLineSetting.TAuditLineLimit)
            .OrderByDescending(row => row.TAuditLineCount)
            .Select(row => $"  {row.TAuditLineCount,5}  {row.TAuditLinePath}")
            .ToList();
        List<string> warnings = TAuditLineRows.Value
            .Where(row => row.TAuditLineCount > TAuditLineSetting.TAuditLineWarning
                && row.TAuditLineCount <= TAuditLineSetting.TAuditLineLimit)
            .OrderByDescending(row => row.TAuditLineCount)
            .Select(row => $"  {row.TAuditLineCount,5}  {row.TAuditLinePath}")
            .ToList();

        TAuditWarningRecord(
            warnings,
            $"file(s) sit within {TAuditLineSetting.TAuditLineLimit - TAuditLineSetting.TAuditLineWarning} lines "
            + $"of the {TAuditLineSetting.TAuditLineLimit}-line limit",
            TAuditLengthAdvice);
        TAuditLineCheck(
            "Length",
            hits,
            $"file(s) exceed the {TAuditLineSetting.TAuditLineLimit}-line limit",
            TAuditLengthAdvice);
    }

    [Fact]
    public void AuditLine_Line_HoldWithinWidth()
    {
        List<string> hits = [];
        List<string> warnings = [];
        foreach (TAuditLineRow row in TAuditLineRows.Value)
        {
            int limit = row.TAuditLineWidth;
            foreach ((int number, int width) in row.TAuditLineWide)
            {
                string line = $"  {width,5}  {row.TAuditLinePath}:{number}";
                (width > limit ? hits : warnings).Add(line);
            }
        }

        TAuditWarningRecord(
            warnings,
            $"line(s) sit within {TAuditLineSetting.TAuditWidthBand} columns of their width limit",
            TAuditWidthAdvice);
        TAuditLineCheck("Width", hits, "line(s) exceed the width limit of their extension", TAuditWidthAdvice);
    }

    [Fact]
    public void AuditLine_Ceiling_MatchesHits()
    {
        Dictionary<string, int> counts = TAuditHitRead();
        List<string> stale = TAuditLineSetting.TAuditLineCeiling
            .Where(pair => counts.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"  {pair.Key}: {counts.GetValueOrDefault(pair.Key)} hit(s), ceiling {pair.Value}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITLINES",
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    private const string TAuditLengthAdvice =
        "Do NOT split the file mechanically, and do NOT add a partial part: both leave one big object behind\n"
        + "several files, which AUDITOBJECT then reports as a monolith. A file this long almost always carries\n"
        + "more than one responsibility. Reconsider the design first: name the second responsibility and give it\n"
        + "a single-purpose type of its own (C-NLRF-2, C-SRFR). A documented exception is rare (C-EXRE).";

    private const string TAuditWidthAdvice =
        "Do NOT wrap the line mechanically. A line this wide usually nests too deep, chains too far or spells\n"
        + "out too long a path. Reconsider the code first: lift a local, split the expression, shorten the chain\n"
        + "or move the work into a named helper.";

    private static Dictionary<string, int> TAuditHitRead()
    {
        IReadOnlyList<TAuditLineRow> rows = TAuditLineRows.Value;
        return new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Length"] = rows.Count(row => row.TAuditLineCount > TAuditLineSetting.TAuditLineLimit),
            ["Width"] = rows.Sum(row => row.TAuditLineWide.Count(pair => pair.TAuditWidth > row.TAuditLineWidth)),
        };
    }

    private void TAuditWarningRecord(List<string> warnings, string summary, string advice)
    {
        if (warnings.Count == 0)
        {
            return;
        }

        _tAuditOutput.WriteLine(TAuditConvention.TAuditReportFormat(
            "AUDITLINES", $"WARNING (not a failure): {warnings.Count} {summary}."));
        _tAuditOutput.WriteLine(advice);
        _tAuditOutput.WriteLine(string.Join('\n', warnings));
        _tAuditOutput.WriteLine("");
    }

    private void TAuditLineCheck(string kind, List<string> hits, string summary, string advice)
    {
        int ceiling = TAuditLineSetting.TAuditLineCeiling.GetValueOrDefault(kind);
        _tAuditOutput.WriteLine($"AUDITLINES {kind}: {hits.Count} {summary}, ceiling {ceiling}.");

        bool held = !TAuditLineSetting.TAuditLineEnforced || hits.Count <= ceiling;
        Assert.True(held, TAuditConvention.TAuditReportFormat(
            "AUDITLINES",
            $"{hits.Count} {summary}, above the ceiling of {ceiling}.\n{advice}\n{string.Join('\n', hits)}"));
    }

    private static IReadOnlyList<TAuditLineRow> TAuditLineRead()
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        List<string> missing = TAuditLineSetting.TAuditLineRoots
            .Where(root => !Directory.Exists(Path.Combine(repoRoot, root)))
            .ToList();
        Assert.True(missing.Count == 0, TAuditConvention.TAuditReportFormat(
            "AUDITLINES", $"{missing.Count} configured root(s) have no directory: {string.Join(", ", missing)}."));

        TAuditScope scope = new(
            TAuditLineSetting.TAuditLineRoots,
            TAuditLineSetting.TAuditLineInclude,
            TAuditLineSetting.TAuditLineSegments,
            [],
            [],
            []);
        IReadOnlyList<string> sources = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(sources.Count > 0, TAuditConvention.TAuditReportFormat(
            "AUDITLINES", "No tracked source file was enumerated; the audit would pass vacuously."));

        List<TAuditLineRow> rows = [];
        foreach (string path in sources)
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            int limit = TAuditLineSetting.TAuditWidthLimit.GetValueOrDefault(Path.GetExtension(path), int.MaxValue);
            int count = 0;
            List<(int TAuditNumber, int TAuditWidth)> wide = [];
            foreach (string line in File.ReadLines(path))
            {
                count++;
                if (limit != int.MaxValue && line.Length > limit - TAuditLineSetting.TAuditWidthBand)
                {
                    wide.Add((count, line.Length));
                }
            }

            rows.Add(new TAuditLineRow(relative, count, limit, wide));
        }

        return rows;
    }
}
