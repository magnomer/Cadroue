using Xunit;

namespace Convention.Tests;

public sealed class TAuditBorder
{
    private const string TAuditBorderAudit = "AUDITBORDER";

    private static readonly string[] TAuditBorderHeld =
        ["Trespassing", "Leapfrogging", "Undercutting", "Leaking", "Unsealing"];

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditBorderHits =
        new(() =>
        [
            .. TAuditBorderWalker.TAuditRun(),
            .. TAuditBorderWalker.TAuditOfferScan(),
            .. TAuditBorderWalker.TAuditSealScan(),
            .. TAuditBorderDrift.TAuditDriftScan(),
            .. TAuditBorderLinger.TAuditLingerScan(),
        ]);

    [Fact]
    public void AuditBorder_Rings_NameOneNeighbour()
    {
        List<string> wide = [];
        foreach ((string ring, string[] neighbour) in TAuditBorderSetting.TAuditBorderNeighbour)
        {
            if (neighbour.Length > 1)
            {
                wide.Add($"  {ring} names neighbours {string.Join(", ", neighbour)}");
            }

            if (neighbour.Contains(ring, StringComparer.Ordinal))
            {
                wide.Add($"  {ring} names itself as neighbour");
            }

            IEnumerable<string> unknown = neighbour
                .Where(target => !TAuditBorderSetting.TAuditBorderNeighbour.ContainsKey(target));
            foreach (string target in unknown)
            {
                wide.Add($"  {ring} names an undeclared neighbour {target}");
            }
        }

        foreach ((string ring, string capsule) in TAuditBorderSetting.TAuditBorderCapsule)
        {
            if (TAuditBorderSetting.TAuditBorderNeighbour.GetValueOrDefault(capsule) is not [])
            {
                wide.Add($"  {ring} names capsule {capsule}, which is undeclared or has a neighbour");
            }

            if (TAuditBorderSetting.TAuditBorderCapsule.Values.Count(value => value == capsule) > 1)
            {
                wide.Add($"  capsule {capsule} is named by more than one ring");
            }
        }

        Assert.True(wide.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditBorderAudit,
            $"{wide.Count} ring(s) name more than one neighbour, themselves or an unknown one:\n"
            + string.Join('\n', wide)));
    }

    [Fact]
    public void AuditBorder_Cut_MatchesShellRoots()
    {
        string[] shells = TAuditBinder.TAuditRootRead(
                [.. TAuditTruthSetting.TAuditShellInclude, .. TAuditTruthSetting.TAuditCapsuleInclude])
            .Select(root => root[(root.LastIndexOf('/') + 1)..])
            .Order(StringComparer.Ordinal)
            .ToArray();
        string[] cut = TAuditBorderSetting.TAuditBorderCut.Order(StringComparer.Ordinal).ToArray();
        Assert.True(shells.SequenceEqual(cut, StringComparer.Ordinal), TAuditConvention.TAuditReportFormat(
            TAuditBorderAudit,
            $"The cut holds [{string.Join(", ", cut)}] but the UI rings are [{string.Join(", ", shells)}]."));
    }

    [Fact]
    public void AuditBorder_Sources_HoldNoLeapfrogging()
    {
        TAuditPair.TAuditPairCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, "Leapfrogging",
            TAuditBorderSetting.TAuditBorderCeiling, TAuditBorderSetting.TAuditBorderExempt,
            "Leapfrogging behaviour name(s) past the neighbour ring");
    }

    [Fact]
    public void AuditBorder_Sources_HoldNoTrespassing()
    {
        TAuditPair.TAuditPairCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, "Trespassing",
            TAuditBorderSetting.TAuditBorderCeiling, TAuditBorderSetting.TAuditBorderExempt,
            "Trespassing outer ring name(s) inside an inner ring");
    }

    [Fact]
    public void AuditBorder_Sources_HoldNoUndercutting()
    {
        TAuditPair.TAuditPairCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, "Undercutting",
            TAuditBorderSetting.TAuditBorderCeiling, TAuditBorderSetting.TAuditBorderExempt,
            "Undercutting name(s) from below the cut inside a UI ring");
    }

    [Fact]
    public void AuditBorder_OfferSignatures_HoldNoLeaking()
    {
        TAuditPair.TAuditPairCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, "Leaking",
            TAuditBorderSetting.TAuditBorderCeiling, TAuditBorderSetting.TAuditBorderExempt,
            "Leaking offer signature type(s) from below Conduct");
    }

    [Fact]
    public void AuditBorder_SealedTypes_HoldNoUnsealing()
    {
        TAuditPair.TAuditPairCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, "Unsealing",
            TAuditBorderSetting.TAuditBorderCeiling, TAuditBorderSetting.TAuditBorderExempt,
            "Unsealing sealed member type(s) from below the neighbour ring");
    }

    [Fact]
    public void AuditBorder_Sources_HoldNoPoaching()
    {
        List<string> hits = [];
        foreach ((string pair, string[] offer) in TAuditEngineSetting.TAuditEngineOffer
                     .Concat(TAuditDeportmentSetting.TAuditDeportmentOffer)
                     .Concat(TAuditDemeanorSetting.TAuditDemeanorOffer))
        {
            hits.AddRange(TAuditBorderHits.Value
                .Where(hit => hit.TAuditHitKind == "Commuting" && hit.TAuditPairRead() == "Commuting:" + pair)
                .Where(hit => !offer.Contains(hit.TAuditHitName, StringComparer.Ordinal))
                .Where(hit => hit.TAuditExemptRead(TAuditBorderSetting.TAuditBorderExempt).Length == 0)
                .Select(hit => $"  {hit.TAuditHitPath}:{hit.TAuditHitLine} {hit.TAuditHitName} outside {pair}"));
        }

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditBorderAudit,
            $"{hits.Count} Poaching neighbour name(s) sit outside the offer a ring may name:\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditBorder_Offers_HoldNoDrifting()
    {
        List<string> hits = TAuditBorderHits.Value
            .Where(hit => hit.TAuditHitKind == "Drifting")
            .Where(hit => hit.TAuditExemptRead(TAuditBorderSetting.TAuditBorderExempt).Length == 0)
            .Select(hit => $"  {hit.TAuditHitPath} {hit.TAuditHitRing}>{hit.TAuditHitTarget} {hit.TAuditHitName}")
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditBorderAudit,
            $"{hits.Count} Drifting hit(s) where an offer list differs from its neighbour:\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditBorder_Subscriptions_HoldNoLingering()
    {
        List<string> hits = TAuditBorderHits.Value
            .Where(hit => hit.TAuditHitKind == "Lingering")
            .Where(hit => hit.TAuditExemptRead(TAuditBorderSetting.TAuditBorderExempt).Length == 0)
            .Select(hit => $"  {hit.TAuditHitPath}:{hit.TAuditHitLine} {hit.TAuditHitName} never removed")
            .ToList();

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditBorderAudit,
            $"{hits.Count} Lingering engine event subscription(s) the subscribing Conduct type never removes:\n"
            + string.Join('\n', hits)));
    }

    [Fact]
    public void AuditBorder_DriftedOffer_ReportsEachBreak()
    {
        IReadOnlyList<TAuditHit> hits = TAuditBorderDrift.TAuditDriftRead(
            new Dictionary<string, string[]>
            {
                ["Cadroue.UIDeportment>Cadroue.Conduct"] = ["CSentence", "CStale", "CHidden", "LDisplay"],
            },
            ["Cadroue.UIDeportment"],
            new Dictionary<string, string[]> { ["Cadroue.Conduct"] = ["C"] },
            [
                ("CSentence", "Cadroue.Conduct", true),
                ("CHidden", "Cadroue.Conduct", false),
                ("LDisplay", "Cadroue.Conduct", true),
                ("CMissing", "Cadroue.Conduct", true),
                ("LTenure", "Cadroue.ShellEngine", true),
            ]);
        string[] expected =
        [
            "CHidden (a) is not public",
            "CMissing (b) is public in Cadroue.Conduct but not offered",
            "CStale (a) resolves to no type",
            "LDisplay (a) lacks the prefix C",
        ];

        Assert.Equal(expected, hits.Select(hit => hit.TAuditHitName));
        Assert.All(hits, hit => Assert.Equal("Drifting:Cadroue.UIDeportment>Cadroue.Conduct", hit.TAuditPairRead()));
    }

    [Fact]
    public void AuditBorder_EqualOffer_ReportsNoDrifting()
    {
        IReadOnlyList<TAuditHit> hits = TAuditBorderDrift.TAuditDriftRead(
            new Dictionary<string, string[]>
            {
                ["Cadroue.UIDeportment>Cadroue.Conduct"] = ["CSentence", "CMissing"],
                ["Cadroue.UIDemeanor>Cadroue.Conduct"] = ["CMissing", "CSentence"],
            },
            ["Cadroue.UIDeportment", "Cadroue.UIDemeanor"],
            new Dictionary<string, string[]> { ["Cadroue.Conduct"] = ["C"] },
            [
                ("CSentence", "Cadroue.Conduct", true),
                ("CHidden", "Cadroue.Conduct", false),
                ("CMissing", "Cadroue.Conduct", true),
                ("LTenure", "Cadroue.ShellEngine", true),
            ]);

        Assert.Empty(hits);
    }

    [Fact]
    public void AuditBorder_Ceiling_MatchesHits()
    {
        TAuditPair.TAuditStaleCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, TAuditBorderHeld,
            TAuditBorderSetting.TAuditBorderCeiling, TAuditBorderSetting.TAuditBorderExempt);
    }

    [Fact]
    public void AuditBorder_Exempt_MatchesSource()
    {
        TAuditPair.TAuditExemptCheck(
            TAuditBorderAudit, TAuditBorderHits.Value, TAuditBorderSetting.TAuditBorderExempt);
    }
}
