using Xunit;

namespace Cadroue.Tests;

public sealed class FlawScanFilterTests
{
    [Fact]
    public void KindsResolve_EmptySet_KeepsAll()
    {
        IReadOnlyList<string> filtered = TFlaw.KindsResolve(
            new[] { "LFlawKindContainer", "LFlawKindTiming" },
            Array.Empty<string>());

        Assert.Equal(new[] { "LFlawKindContainer", "LFlawKindTiming" }, filtered);
    }

    [Fact]
    public void KindsResolve_RequestedKinds_KeepsOnlyThose()
    {
        IReadOnlyList<string> filtered = TFlaw.KindsResolve(
            new[] { "LFlawKindContainer", "LFlawKindTiming", "LFlawKindCoded" },
            new[] { "LFlawKindTiming", "LFlawKindCoded" });

        Assert.Equal(new[] { "LFlawKindTiming", "LFlawKindCoded" }, filtered);
    }
}
