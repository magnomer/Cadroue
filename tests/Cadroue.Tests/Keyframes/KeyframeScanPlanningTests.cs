using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class KeyframeScanPlanningTests
{
    [Fact]
    public async Task FullyCachedRequestedRange_RequiresNoAdditionalScan()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("full.mp4", "full source");
        Assert.True(keyframes.CacheSave(source, TimeSpan.FromSeconds(60), Array.Empty<long>(), new[] { 0, 1, 2 }));

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframes.SettleAsync();

        Assert.Equal(0, keyframes.ScanCount);
        Assert.Equal(3, keyframes.Latest!.Coverage.Count);
    }

    [Fact]
    public async Task PartiallyCachedRange_RequestsOnlyMissingProductionSpan()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("partial.mp4", "partial source");
        Assert.True(keyframes.CacheSave(source, TimeSpan.FromSeconds(60), Array.Empty<long>(), new[] { 0, 2 }));

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForCoverageCountAsync(3);

        TKeyframeRange scan = Assert.Single(keyframes.Scans);
        Assert.Equal(new TKeyframeRange(20_000, 40_000), scan);
    }

    [Fact]
    public async Task OverlappingCachedCoverage_IsInterpretedWithoutRescanningDuplicates()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("cached-overlap.mp4", "cached overlap source");
        Assert.True(keyframes.CacheSave(source, TimeSpan.FromSeconds(80), Array.Empty<long>(), new[] { 0, 1, 1, 2 }));

        keyframes.Start(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(30));
        await keyframes.WaitForCoverageCountAsync(4);

        Assert.Equal(new[] { new TKeyframeRange(60_000, 80_000) }, keyframes.Scans);
    }

    [Fact]
    public async Task RepeatedSatisfiedRequest_DoesNotNeedlesslyRescan()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("repeat.mp4", "repeat source");

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForCoverageCountAsync(3);
        int firstScanCount = keyframes.ScanCount;

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframes.SettleAsync();

        Assert.Equal(3, firstScanCount);
        Assert.Equal(firstScanCount, keyframes.ScanCount);
    }
}
