using Xunit;

namespace Cadroue.Tests;

public sealed class LosslessCutValidationTests
{
    [Theory]
    [InlineData("{\"start\":-1,\"end\":2}")]
    [InlineData("{\"start\":0,\"end\":-1}")]
    public void NegativeBoundary_IsRejected(string segment)
    {
        TLosslessCut.TResult result = Validate(segment, 10_000);

        Assert.Empty(result.Sections);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void EndBeforeStart_IsRejected()
    {
        TLosslessCut.TResult result = Validate("""{"start":5,"end":4}""", 10_000);

        Assert.Empty(result.Sections);
        Assert.Contains(result.Issues, issue => issue.Reason.Contains("end does not follow start"));
    }

    [Fact]
    public void EndBeyondKnownMediaDuration_IsRejected()
    {
        TLosslessCut.TResult result = Validate("""{"start":1,"end":11}""", 10_000);

        Assert.Empty(result.Sections);
        Assert.Contains(result.Issues, issue => issue.Reason.Contains("exceeds"));
    }

    [Fact]
    public void OmittedEndWithoutKnownDuration_IsRejected()
    {
        TLosslessCut.TResult result = Validate("""{"start":1}""", 0);

        Assert.Empty(result.Sections);
        Assert.Contains(result.Issues, issue => issue.Reason.Contains("duration is unavailable"));
    }

    private static TLosslessCut.TResult Validate(string segment, long durationMilliseconds)
    {
        TLosslessCut.TProject project = TLosslessCut.Parse(
            $$"""{"version":1,"mediaFileName":"clip.mp4","cutSegments":[{{segment}}]}""");
        return TLosslessCut.Validate(
            project,
            "clip.mp4",
            TimeSpan.FromMilliseconds(durationMilliseconds));
    }
}
