using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditChain
{
    private const string TAuditChainAudit = "AUDITCHAIN";

    private static readonly string[] TAuditChainHeld = ["outward", "reach"];

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditChainHits = new(TAuditChainWalker.TAuditRun);

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

            IEnumerable<string> unknown = reach
                .Where(target => !TAuditChainSetting.TAuditChainReach.ContainsKey(target));
            foreach (string target in unknown)
            {
                wide.Add($"  {ring} reaches an undeclared ring {target}");
            }
        }

        Assert.True(wide.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditChainAudit,
            $"{wide.Count} ring(s) reach more than one ring or an unknown one:\n{string.Join('\n', wide)}"));
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
    public void AuditChain_Sources_NameNoOuterRing()
    {
        TAuditPair.TAuditPairCheck(
            TAuditChainAudit, TAuditChainHits.Value, "outward",
            TAuditChainSetting.TAuditChainCeiling, TAuditChainSetting.TAuditChainWaiver,
            "outer ring name(s) inside an inner ring");
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
