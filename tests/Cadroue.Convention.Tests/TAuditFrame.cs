using Xunit;

namespace Convention.Tests;

public sealed class TAuditFrame
{
    private const string TAuditFrameAudit = "AUDITFRAME";

    private static readonly string[] TAuditFrameHeld = ["frame", "ambient"];

    private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditFrameHits = new(TAuditFrameWalker.TAuditRun);

    [Fact]
    public void AuditFrame_PureSources_StayInFrame()
    {
        TAuditPair.TAuditPairCheck(
            TAuditFrameAudit, TAuditFrameHits.Value, "frame",
            TAuditFrameSetting.TAuditFrameCeiling, TAuditFrameSetting.TAuditFrameWaiver,
            "framework namespace(s) outside the frame");
    }

    [Fact]
    public void AuditFrame_PureSources_TouchNoAmbient()
    {
        TAuditPair.TAuditPairCheck(
            TAuditFrameAudit, TAuditFrameHits.Value, "ambient",
            TAuditFrameSetting.TAuditFrameCeiling, TAuditFrameSetting.TAuditFrameWaiver,
            "ambient member(s) touched");
    }

    [Fact]
    public void AuditFrame_Ceiling_MatchesHits()
    {
        TAuditPair.TAuditStaleCheck(
            TAuditFrameAudit, TAuditFrameHits.Value, TAuditFrameHeld,
            TAuditFrameSetting.TAuditFrameCeiling, TAuditFrameSetting.TAuditFrameWaiver);
    }

    [Fact]
    public void AuditFrame_Waiver_MatchesSource()
    {
        TAuditPair.TAuditWaiverCheck(TAuditFrameAudit, TAuditFrameHits.Value, TAuditFrameSetting.TAuditFrameWaiver);
    }
}
