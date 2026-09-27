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
            new[] { 0, 1, 2, 3, 4, 5 }));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(0, keyframes.TKeyframeScanCount);
        Assert.Equal(6, keyframes.TKeyframeLatest!.TKeyframeCoverage.Count);
    }

    [Fact]
    public async Task LegacyTwentySecondSpans_AreReadAsTenSecondWindows()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("legacy.mp4", "legacy source");
        Assert.True(keyframes.TKeyframeCacheSave(
            source,
            TimeSpan.FromSeconds(60),
            Array.Empty<long>(),
            new[] { 0, 2 },
            20_000));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(6);

        Assert.Equal(
            new[] { new TKeyframeRange(30_000, 40_000), new TKeyframeRange(20_000, 30_000) },
            keyframes.TKeyframeScans);
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
            new[] { 0, 1, 2, 4, 5 }));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(6);

        TKeyframeRange scan = Assert.Single(keyframes.TKeyframeScans);
        Assert.Equal(new TKeyframeRange(30_000, 40_000), scan);
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
            new[] { 0, 1, 1, 2, 3, 4, 5, 6 }));

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(8);

        Assert.Equal(new[] { new TKeyframeRange(70_000, 80_000) }, keyframes.TKeyframeScans);
    }

    [Fact]
    public async Task RepeatedSatisfiedRequest_DoesNotNeedlesslyRescan()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("repeat.mp4", "repeat source");

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(6);
        int firstScanCount = keyframes.TKeyframeScanCount;

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(6, firstScanCount);
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
        keyframes.TKeyframeResultSet(source, 30_000, 30_040, 30_080);
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
    public async Task CursorMoveDuringScan_WaitsForSpan_ThenContinuesFromLatestCursor()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("moving.mp4", "moving source");
        keyframes.TKeyframeScanSuspend(source, honorCancellation: true);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(1);
        keyframes.TKeyframeSync(TimeSpan.FromSeconds(50));
        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(50));
        keyframes.TKeyframeSync(TimeSpan.FromSeconds(70));
        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(80), TimeSpan.FromSeconds(70));
        await TKeyframe.TKeyframeSettleRun();
        Assert.Equal(1, keyframes.TKeyframeScanCount);

        keyframes.TKeyframeScanRelease(source);
        await keyframes.TKeyframeCoverageRead(8);

        Assert.Equal(new TKeyframeRange(30_000, 40_000), keyframes.TKeyframeScans[0]);
        Assert.Equal(new TKeyframeRange(70_000, 80_000), keyframes.TKeyframeScans[1]);
        Assert.Equal(1, keyframes.TKeyframeScans.Count(scan => scan.TKeyframeStartMilliseconds == 30_000));
    }

    [Fact]
    public async Task ScanOrder_StepsOutwardInTenSecondWindows_AfterFirst()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("outward.mp4", "outward source");

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(105));
        await keyframes.TKeyframeCoverageRead(20);

        Assert.Equal(
            new[] { 10, 9, 11, 8, 12, 7, 13, 6, 14, 5, 15, 4, 16, 3, 17, 2, 18, 1, 19, 0 },
            keyframes.TKeyframeScans.Select(scan => (int)(scan.TKeyframeStartMilliseconds / 10_000)));
    }

    [Fact]
    public async Task ScanOrder_AfterBackwardMove_ScansBeforeFirst()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("backward.mp4", "backward source");

        _ = keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(105), -1);
        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(200), TimeSpan.FromSeconds(105));
        await keyframes.TKeyframeCoverageRead(4);

        Assert.Equal(
            new[] { 9, 10, 8, 11 },
            keyframes.TKeyframeScans.Take(4).Select(scan => (int)(scan.TKeyframeStartMilliseconds / 10_000)));
    }

    [Fact]
    public async Task ScanWindow_StopsAtTwoMinutesEachSide()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("window.mp4", "window source");

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(300));
        await keyframes.TKeyframeCoverageRead(24);
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(24, keyframes.TKeyframeScanCount);
        Assert.Equal(180_000, keyframes.TKeyframeScans.Min(scan => scan.TKeyframeStartMilliseconds));
        Assert.Equal(420_000, keyframes.TKeyframeScans.Max(scan => scan.TKeyframeEndMilliseconds));
    }

    [Fact]
    public async Task NextKeyframe_BeyondScanWindow_ScansOnwardUntilFound()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("reach.mp4", "reach source");
        keyframes.TKeyframeResultSet(source, 455_000);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(300));
        await keyframes.TKeyframeCoverageRead(24);
        await TKeyframe.TKeyframeSettleRun();
        Assert.False(keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(300), 1).LKeyframeReady);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(600), TimeSpan.FromSeconds(300));
        await keyframes.TKeyframeCoverageRead(28);

        LKeyframeMoveResult next = keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(300), 1);
        Assert.True(next.LKeyframeReady);
        Assert.Equal(TimeSpan.FromSeconds(455), next.LKeyframeTarget);
    }
}
