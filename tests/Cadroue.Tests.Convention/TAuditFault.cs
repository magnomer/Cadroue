using Xunit;

namespace Convention.Tests;

public sealed class TAuditFault
{
    private const string TAuditFaultAudit = "AUDITFAULT";

    private static readonly Lazy<IReadOnlyList<TViolation>> TAuditFaultHits = new(TAuditFaultWalker.TAuditFaultScan);

    [Fact]
    public void AuditFault_Catches_HoldWithinCeiling()
    {
        List<string> above = TAuditAboveRead();

        Assert.True(above.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditFaultAudit,
            $"Rings swallow faults above their ceiling:\n{string.Join('\n', above)}"));
    }

    [Fact]
    public void AuditFault_Ceiling_MatchesHits()
    {
        List<string> stale = TAuditStaleRead();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditFaultAudit,
            $"{stale.Count} ceiling(s) sit above the count and must be lowered.\n{string.Join('\n', stale)}"));
    }

    [Fact]
    public void AuditFault_Exempt_MatchesSource()
    {
        HashSet<string> used = TAuditFaultHits.Value
            .Select(TAuditExemptRead)
            .Where(row => row.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        List<string> stale = TAuditFaultSetting.TAuditFaultExempt
            .Where(row => !used.Contains(row))
            .Select(row => $"  {row}")
            .ToList();

        Assert.True(stale.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditFaultAudit,
            $"{stale.Count} exempt row(s) match no source line and must be deleted:\n{string.Join('\n', stale)}"));
    }

    private static string TAuditExemptRead(TViolation hit)
    {
        string row = $"{hit.TViolationPath}:{hit.TViolationName}";
        return TAuditFaultSetting.TAuditFaultExempt.Contains(row, StringComparer.Ordinal) ? row : "";
    }

    private static List<TViolation> TAuditTallyRead(string ring)
    {
        return TAuditFaultHits.Value
            .Where(hit => TAuditExemptRead(hit).Length == 0)
            .Where(hit => string.Equals(
                TAuditBinder.TAuditRingRead(hit.TViolationPath, TAuditFaultSetting.TAuditFaultRing),
                ring,
                StringComparison.Ordinal))
            .ToList();
    }

    private static List<string> TAuditAboveRead()
    {
        List<string> above = [];
        foreach (string ring in TAuditFaultSetting.TAuditFaultRing)
        {
            List<TViolation> hits = TAuditTallyRead(ring);
            int ceiling = TAuditFaultSetting.TAuditFaultCeiling.GetValueOrDefault(ring);
            if (hits.Count <= ceiling)
            {
                continue;
            }

            above.Add($"  {ring}: {hits.Count} hit(s), ceiling {ceiling}");
            above.AddRange(hits.Select(hit => $"    {hit.TViolationPath}:{hit.TViolationLine} {hit.TViolationReason}"));
        }

        return above;
    }

    private static List<string> TAuditStaleRead()
    {
        return TAuditFaultSetting.TAuditFaultCeiling
            .Where(pair => TAuditTallyRead(pair.Key).Count < pair.Value)
            .Select(pair => $"  {pair.Key}: {TAuditTallyRead(pair.Key).Count} hit(s), ceiling {pair.Value}")
            .ToList();
    }
}
