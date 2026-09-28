using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditChain
{
    private const string TAuditChainAudit = "AUDITCHAIN";

    private static readonly string[] TAuditChainHeld = ["outward", "reach", "cross", "expose", "seal"];

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditChainHits =
        new(() =>
        [
            .. TAuditChainWalker.TAuditRun(),
            .. TAuditChainWalker.TAuditSurfaceScan(),
            .. TAuditChainWalker.TAuditSealScan(),
        ]);

    [Fact]
    public void AuditChain_Rings_ReachOneRing()
    {
        List<string> wide = [];
        foreach ((string ring, string[] reach) in TAuditChainSetting.TAuditChainReach)
        {
            if (reach.Length > 1)
            {
                wide.Add($"  {ring} reaches {string.Join(", ", reach)}");
            }

            if (reach.Contains(ring, StringComparer.Ordinal))
            {
                wide.Add($"  {ring} reaches itself");
            }

            IEnumerable<string> unknown = reach
                .Where(target => !TAuditChainSetting.TAuditChainReach.ContainsKey(target));
            foreach (string target in unknown)
            {
                wide.Add($"  {ring} reaches an undeclared ring {target}");
            }
        }

        foreach ((string ring, string capsule) in TAuditChainSetting.TAuditChainCapsule)
        {
            if (TAuditChainSetting.TAuditChainReach.GetValueOrDefault(capsule) is not [])
            {
                wide.Add($"  {ring} names capsule {capsule}, which is undeclared or reaches a ring");
            }

            if (TAuditChainSetting.TAuditChainCapsule.Values.Count(value => value == capsule) > 1)
            {
                wide.Add($"  capsule {capsule} is named by more than one ring");
            }
        }

        Assert.True(wide.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"{wide.Count} ring(s) reach more than one ring, themselves or an unknown one:\n"
            + string.Join('\n', wide)));
    }

    [Fact]
    public void AuditChain_Reach_MatchesRingTable()
    {
        List<string> drift = [];
        foreach ((string ring, string[] edges) in TAuditRingSetting.TAuditRingEdges.Where(pair =>
                     pair.Key != TAuditChainSetting.TAuditChainHost))
        {
            string[] reach = TAuditChainSetting.TAuditChainReach.GetValueOrDefault(ring) ?? ["(absent)"];
            if (TAuditChainSetting.TAuditChainCapsule.GetValueOrDefault(ring) is string capsule)
            {
                reach = [.. reach, capsule];
            }

            if (!reach.Order(StringComparer.Ordinal).SequenceEqual(edges.Order(StringComparer.Ordinal)))
            {
                drift.Add($"  {ring} reaches [{string.Join(", ", reach)}] but references [{string.Join(", ", edges)}]");
            }
        }

        drift.AddRange(TAuditChainSetting.TAuditChainReach.Keys
            .Where(ring => !TAuditRingSetting.TAuditRingEdges.ContainsKey(ring))
            .Select(ring => $"  {ring} is walked but is no project in the ring table"));
        Assert.True(drift.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"{drift.Count} ring(s) walk a reach that differs from their project references:\n"
            + string.Join('\n', drift)));
    }

    [Fact]
    public void AuditChain_Cut_MatchesShellRoots()
    {
        string[] shells = TAuditBinder.TAuditRootRead(TAuditTruthSetting.TAuditShellInclude)
            .Select(root => root[(root.LastIndexOf('/') + 1)..])
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] cut = TAuditChainSetting.TAuditChainCut.Order(StringComparer.Ordinal).ToArray();
        Assert.True(shells.SequenceEqual(cut, StringComparer.Ordinal), TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"The cut holds [{string.Join(", ", cut)}] but the UI rings are [{string.Join(", ", shells)}]."));
    }

    [Fact]
    public void AuditChain_Sources_ReachNoDeeperRing()
    {
        TAuditPair.TAuditPairCheck(
            TAuditChainAudit, TAuditChainHits.Value, "reach",
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver,
            "behaviour reach(es) past the neighbour ring");
    }

    [Fact]
    public void AuditChain_Folders_HoldNoBannedWord()
    {
        List<string> hits = [];
        foreach ((string folder, string[] words) in TAuditChainSetting.TAuditChainBanned)
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
            TAuditChainAudit,
            $"{hits.Count} line(s) name a word banned from their folder:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditChain_Sources_NameNoOuterRing()
    {
        TAuditPair.TAuditPairCheck(
            TAuditChainAudit, TAuditChainHits.Value, "outward",
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver,
            "outer ring name(s) inside an inner ring");
    }

    [Fact]
    public void AuditChain_Sources_CrossNoCut()
    {
        TAuditPair.TAuditPairCheck(
            TAuditChainAudit, TAuditChainHits.Value, "cross",
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver,
            "name(s) from below the cut inside a UI ring");
    }

    [Fact]
    public void AuditChain_SurfaceSignatures_NameNoDeeperType()
    {
        TAuditPair.TAuditPairCheck(
            TAuditChainAudit, TAuditChainHits.Value, "expose",
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver,
            "surface signature type(s) from below Conduct");
    }

    [Fact]
    public void AuditChain_SealedSurface_NamesNoEngine()
    {
        TAuditPair.TAuditPairCheck(
            TAuditChainAudit, TAuditChainHits.Value, "seal",
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver,
            "sealed member type(s) from below the neighbour ring");
    }

    [Fact]
    public void AuditChain_Sources_HoldSurface()
    {
        List<string> hits = [];
        foreach ((string pair, string[] surface) in TAuditChainSetting.TAuditChainSurface)
        {
            hits.AddRange(TAuditChainHits.Value
                .Where(hit => hit.TAuditHitKind == "neighbour" && hit.TAuditPairRead() == "neighbour:" + pair)
                .Where(hit => !surface.Contains(hit.TAuditHitName, StringComparer.Ordinal))
                .Where(hit => hit.TAuditWaiverRead(TAuditChainSetting.TAuditChainWaiver).Length == 0)
                .Select(hit => $"  {hit.TAuditHitPath}:{hit.TAuditHitLine} {hit.TAuditHitName} outside {pair}"));
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"{hits.Count} neighbour name(s) sit outside the surface a ring may reach:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditChain_Rings_HoldFloor()
    {
        IReadOnlyDictionary<string, int> files = TAuditChainWalker.TAuditFileRead();
        List<string> thin = TAuditChainSetting.TAuditChainFloor
            .Where(pair => files.GetValueOrDefault(pair.Key) < pair.Value)
            .Select(pair => $"  {pair.Key}: {files.GetValueOrDefault(pair.Key)} source file(s), floor {pair.Value}")
            .ToList();

        Assert.True(thin.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"{thin.Count} ring(s) hold fewer source files than their floor:\n{string.Join('\n', thin)}"));
    }

    [Fact]
    public void AuditChain_Rings_DeclareNoStray()
    {
        List<string> stray = [];
        foreach (TAuditHit declared in TAuditChainWalker.TAuditDeclaredRead())
        {
            string[] patterns = TAuditChainSetting.TAuditChainStray.GetValueOrDefault(declared.TAuditHitRing, []);
            foreach (string pattern in patterns.Where(pattern => Regex.IsMatch(declared.TAuditHitName, pattern)))
            {
                stray.Add($"  {declared.TAuditHitPath}:{declared.TAuditHitLine} {declared.TAuditHitName} ~ {pattern}");
            }
        }

        Assert.True(stray.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"{stray.Count} type(s) are declared in a ring that may not hold them:\n{string.Join('\n', stray)}"));
    }

    [Fact]
    public void AuditChain_Ceiling_MatchesHits()
    {
        TAuditPair.TAuditStaleCheck(
            TAuditChainAudit, TAuditChainHits.Value, TAuditChainHeld,
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver);
    }

    [Fact]
    public void AuditChain_Waiver_MatchesSource()
    {
        TAuditPair.TAuditWaiverCheck(TAuditChainAudit, TAuditChainHits.Value, TAuditChainSetting.TAuditChainWaiver);
    }
}
