using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TKeyframeScanPlan
{
    [Fact]
    public async Task FullyCachedRequestedRange_RequiresNoAdditionalScan()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("full.mp4", "full source");
        Assert.True(keyframes.TKeyframeCacheSave(source, TimeSpan.FromSeconds(60), Array.Empty<long>(), new[] { 0, 1, 2 }));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(0, keyframes.TKeyframeScanCount);
        Assert.Equal(3, keyframes.TKeyframeLatest!.TKeyframeCoverage.Count);
    }

    [Fact]
    public async Task PartiallyCachedRange_RequestsOnlyMissingProductionSpan()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("partial.mp4", "partial source");
        Assert.True(keyframes.TKeyframeCacheSave(source, TimeSpan.FromSeconds(60), Array.Empty<long>(), new[] { 0, 2 }));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);

        TKeyframeRange scan = Assert.Single(keyframes.TKeyframeScans);
        Assert.Equal(new TKeyframeRange(20_000, 40_000), scan);
    }

    [Fact]
    public async Task OverlappingCachedCoverage_IsInterpretedWithoutRescanningDuplicates()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("cached-overlap.mp4", "cached overlap source");
        Assert.True(keyframes.TKeyframeCacheSave(source, TimeSpan.FromSeconds(80), Array.Empty<long>(), new[] { 0, 1, 1, 2 }));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(4);

        Assert.Equal(new[] { new TKeyframeRange(60_000, 80_000) }, keyframes.TKeyframeScans);
    }

    [Fact]
    public async Task RepeatedSatisfiedRequest_DoesNotNeedlesslyRescan()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("repeat.mp4", "repeat source");

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);
        int firstScanCount = keyframes.TKeyframeScanCount;

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(3, firstScanCount);
        Assert.Equal(firstScanCount, keyframes.TKeyframeScanCount);
    }
}
