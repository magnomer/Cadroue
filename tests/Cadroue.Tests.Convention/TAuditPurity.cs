using Xunit;

namespace Convention.Tests;

public sealed class TAuditPurity
{
    private const string TAuditPurityAudit = "AUDITPURITY";

    private static readonly string[] TAuditPurityHeld = ["Foraging", "Eavesdropping"];

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditPurityHits = new(TAuditPurityWalker.TAuditRun);

    [Fact]
    public void AuditPurity_PureSources_HoldNoForaging()
    {
        TAuditPair.TAuditPairCheck(
            TAuditPurityAudit, TAuditPurityHits.Value, "Foraging",
            TAuditPuritySetting.TAuditPurityCeiling, TAuditPuritySetting.TAuditPurityExempt,
            "Foraging framework namespace(s) outside the frame");
    }

    [Fact]
    public void AuditPurity_PureSources_HoldNoEavesdropping()
    {
        TAuditPair.TAuditPairCheck(
            TAuditPurityAudit, TAuditPurityHits.Value, "Eavesdropping",
            TAuditPuritySetting.TAuditPurityCeiling, TAuditPuritySetting.TAuditPurityExempt,
            "Eavesdropping member(s) touched");
    }

    [Fact]
    public void AuditPurity_Ceiling_MatchesHits()
    {
        TAuditPair.TAuditStaleCheck(
            TAuditPurityAudit, TAuditPurityHits.Value, TAuditPurityHeld,
            TAuditPuritySetting.TAuditPurityCeiling, TAuditPuritySetting.TAuditPurityExempt);
    }

    [Fact]
    public void AuditPurity_Exempt_MatchesSource()
    {
        TAuditPair.TAuditExemptCheck(TAuditPurityAudit, TAuditPurityHits.Value, TAuditPuritySetting.TAuditPurityExempt);
    }
}
