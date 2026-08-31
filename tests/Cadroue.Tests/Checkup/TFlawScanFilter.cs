using Xunit;

namespace Cadroue.Tests;

public sealed class TFlawScanFilter
{
    [Fact]
    public void KindsResolve_EmptySet_KeepsAll()
    {
        IReadOnlyList<string> filtered = TFlaw.TFlawKindsResolve(
            new[] { "LFlawKindContainer", "LFlawKindTiming" },
            Array.Empty<string>());

        Assert.Equal(new[] { "LFlawKindContainer", "LFlawKindTiming" }, filtered);
    }

    [Fact]
    public void KindsResolve_RequestedKinds_KeepsOnlyThose()
    {
        IReadOnlyList<string> filtered = TFlaw.TFlawKindsResolve(
            new[] { "LFlawKindContainer", "LFlawKindTiming", "LFlawKindCoded" },
            new[] { "LFlawKindTiming", "LFlawKindCoded" });

        Assert.Equal(new[] { "LFlawKindTiming", "LFlawKindCoded" }, filtered);
    }
}
