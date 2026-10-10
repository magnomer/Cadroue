using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditCensus
{
    private const string TAuditCensusAudit = "AUDITCENSUS";

    [Fact]
    public void AuditCensus_Folders_HoldNoSmuggling()
    {
        List<string> hits = [];
        foreach ((string folder, string[] words) in TAuditCensusSetting.TAuditSmugglingWord)
        {
            foreach (SyntaxTree tree in TAuditBinder.TAuditTrees)
            {
                string relative = TAuditBinder.TAuditRelativeRead(tree.FilePath);
                if (!relative.StartsWith(folder.Trim('/') + "/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                TextLineCollection lines = tree.GetText().Lines;
                for (int index = 0; index < lines.Count; index++)
                {
                    string text = lines[index].ToString();
                    hits.AddRange(words
                        .Where(word => text.Contains(word, StringComparison.Ordinal))
                        .Select(word => $"  {relative}:{index + 1} {word}"));
                }
            }
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditCensusAudit,
            $"{hits.Count} Smuggling line(s) name a word listed for their folder:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditCensus_Rings_HoldNoHollowing()
    {
        IReadOnlyDictionary<string, int> files = TAuditCensusWalker.TAuditFileRead();
        List<string> thin = TAuditCensusSetting.TAuditHollowingFloor
            .Where(pair => files.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"  {pair.Key}: {files.GetValueOrDefault(pair.Key)} source file(s), floor {pair.Value}")
            .ToList();

        Assert.True(thin.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditCensusAudit,
            $"{thin.Count} Hollowing ring(s) hold fewer source files than their floor:\n{string.Join('\n', thin)}"));
    }

    [Fact]
    public void AuditCensus_Rings_HoldNoSquatting()
    {
        List<string> squatting = [];
        foreach (TAuditHit declared in TAuditCensusWalker.TAuditDeclaredRead())
        {
            string[] patterns =
                TAuditCensusSetting.TAuditSquattingPattern.GetValueOrDefault(declared.TAuditHitRing, []);
            foreach (string pattern in patterns.Where(pattern => Regex.IsMatch(declared.TAuditHitName, pattern)))
            {
                squatting.Add(
                    $"  {declared.TAuditHitPath}:{declared.TAuditHitLine} {declared.TAuditHitName} ~ {pattern}");
            }
        }

        Assert.True(squatting.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditCensusAudit,
            $"{squatting.Count} Squatting type(s) are declared in a ring that may not hold them:\n"
            + string.Join('\n', squatting)));
    }
}
