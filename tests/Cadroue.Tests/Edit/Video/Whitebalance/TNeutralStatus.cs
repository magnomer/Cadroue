using Cadroue.Application;

using Xunit;

namespace Cadroue.Tests;

public sealed class TNeutralStatus
{
    [Fact]
    public void Resolved_MapsToApply()
    {
        Assert.Equal(
            LNeutralStatus.LNeutralStatusValid,
            TNeutral.TNeutralStatusResolve(LNeutralOutcome.LNeutralOutcomeResolved));
    }

    [Fact]
    public void Decode_MapsToDecode()
    {
        Assert.Equal(
            LNeutralStatus.LNeutralStatusDecode,
            TNeutral.TNeutralStatusResolve(LNeutralOutcome.LNeutralOutcomeDecode));
    }

    [Theory]
    [InlineData(LNeutralOutcome.LNeutralOutcomeDark)]
    [InlineData(LNeutralOutcome.LNeutralOutcomeEmpty)]
    [InlineData(LNeutralOutcome.LNeutralOutcomeOutside)]
    public void DarkOrInvalidSample_MapsToInvalid(LNeutralOutcome outcome)
    {
        Assert.Equal(LNeutralStatus.LNeutralStatusInvalid, TNeutral.TNeutralStatusResolve(outcome));
    }
}
