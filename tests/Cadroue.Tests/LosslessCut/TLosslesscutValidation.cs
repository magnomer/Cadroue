using Xunit;

namespace Cadroue.Tests;

public sealed class TLosslesscutValidation
{
    [Theory]
    [InlineData("{\"start\":-1,\"end\":2}")]
    [InlineData("{\"start\":0,\"end\":-1}")]
    public void NegativeBoundary_IsRejected(string segment)
    {
        TLosslesscut.TLosslesscutResult result = TLosslesscutValidate(segment, 10_000);

        Assert.Empty(result.TLosslesscutSections);
        Assert.Single(result.TLosslesscutIssues);
    }

    [Fact]
    public void EndBeforeStart_IsRejected()
    {
        TLosslesscut.TLosslesscutResult result = TLosslesscutValidate("""{"start":5,"end":4}""", 10_000);

        Assert.Empty(result.TLosslesscutSections);
        Assert.Contains(result.TLosslesscutIssues, issue => issue.TLosslesscutReason.Contains("end does not follow start"));
    }

    [Fact]
    public void EndBeyondKnownMediaDuration_IsRejected()
    {
        TLosslesscut.TLosslesscutResult result = TLosslesscutValidate("""{"start":1,"end":11}""", 10_000);

        Assert.Empty(result.TLosslesscutSections);
        Assert.Contains(result.TLosslesscutIssues, issue => issue.TLosslesscutReason.Contains("exceeds"));
    }

    [Fact]
    public void OmittedEndWithoutKnownDuration_IsRejected()
    {
        TLosslesscut.TLosslesscutResult result = TLosslesscutValidate("""{"start":1}""", 0);

        Assert.Empty(result.TLosslesscutSections);
        Assert.Contains(result.TLosslesscutIssues, issue => issue.TLosslesscutReason.Contains("duration is unavailable"));
    }

    private static TLosslesscut.TLosslesscutResult TLosslesscutValidate(string segment, long durationMilliseconds)
    {
        TLosslesscut.TLosslesscutProject project = TLosslesscut.TLosslesscutParse(
            $$"""{"version":1,"mediaFileName":"clip.mp4","cutSegments":[{{segment}}]}""");
        return TLosslesscut.TLosslesscutValidate(
            project,
            "clip.mp4",
            TimeSpan.FromMilliseconds(durationMilliseconds));
    }
}
