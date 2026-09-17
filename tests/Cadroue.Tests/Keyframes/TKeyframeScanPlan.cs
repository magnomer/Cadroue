using Cadroue.Core;
using Cadroue.Infrastructure;

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
        Assert.True(keyframes.TKeyframeCacheSave(
            source,
            TimeSpan.FromSeconds(60),
            Array.Empty<long>(),
            new[] { 0, 1, 2 }));

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
        Assert.True(keyframes.TKeyframeCacheSave(
            source,
            TimeSpan.FromSeconds(60),
            Array.Empty<long>(),
            new[] { 0, 2 }));

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
        Assert.True(keyframes.TKeyframeCacheSave(
            source,
            TimeSpan.FromSeconds(80),
            Array.Empty<long>(),
            new[] { 0, 1, 1, 2 }));

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

    [Fact]
    public async Task IntraOnlyCodec_NeedsNoScan_AndStepsOneFrame()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("intra.mov", "prores source");

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30), "prores");
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(0, keyframes.TKeyframeScanCount);
        Assert.Equal(LKeyframeKind.LKeyframeKindIntra, keyframes.TKeyframeLatest!.TKeyframeKind);
        Assert.Equal(new[] { new TKeyframeRange(0, 60_000) }, keyframes.TKeyframeLatest.TKeyframeCoverage);
        LKeyframeMoveResult next = keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(30), 1);
        Assert.True(next.LKeyframeReady);
        Assert.Equal(TimeSpan.FromSeconds(30.04), next.LKeyframeTarget);
    }

    [Fact]
    public async Task AudioOnlySource_NeedsNoScan()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("audio.flac", "audio source");

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30), video: false);
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(0, keyframes.TKeyframeScanCount);
        Assert.Equal(LKeyframeKind.LKeyframeKindNone, keyframes.TKeyframeLatest!.TKeyframeKind);
        Assert.Null(keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(30), 1).LKeyframeTarget);
    }

    [Fact]
    public async Task EveryPacketKeyframe_PromotesSourceToIntra_AndStopsScanning()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("allintra.mp4", "all intra h264");
        keyframes.TKeyframeResultSet(source, 25_000, 25_040, 25_080);
        keyframes.TKeyframeIntraSet(source);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(1);
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(1, keyframes.TKeyframeScanCount);
        Assert.Equal(LKeyframeKind.LKeyframeKindIntra, keyframes.TKeyframeLatest!.TKeyframeKind);
        Assert.Empty(keyframes.TKeyframeLatest.TKeyframeList);
        Assert.Null(keyframes.TKeyframeCacheLoad(source, TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public async Task CursorMoveDuringScan_ContinuesWithoutRestartingSpans()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("moving.mp4", "moving source");
        keyframes.TKeyframeScanSuspend(source, honorCancellation: true);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(1);
        keyframes.TKeyframeSuspend();
        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(70));
        keyframes.TKeyframeScanRelease(source);
        await keyframes.TKeyframeCoverageRead(4);

        Assert.Equal(4, keyframes.TKeyframeScanCount);
        Assert.Equal(new TKeyframeRange(20_000, 40_000), keyframes.TKeyframeScans[0]);
        Assert.Equal(new TKeyframeRange(60_000, 80_000), keyframes.TKeyframeScans[1]);
    }
}
