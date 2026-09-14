using Cadroue.Infrastructure;

using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class TKeyframeOrchestration
{
    [Fact]
    public async Task CancelledScanning_DoesNotPublishSuccessfulCompleteResult()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("cancel.mp4", "cancel source");
        keyframes.TKeyframeResultSet(source, 5_000, 25_000, 45_000);
        keyframes.TKeyframeScanSuspend(source, honorCancellation: true);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(1);
        keyframes.TKeyframeSuspend();
        keyframes.TKeyframeScanRelease(source);
        await TKeyframe.TKeyframeSettleRun();

        Assert.DoesNotContain(keyframes.TKeyframeNotices, notice => notice.TKeyframeCoverage.Count == 3);
    }

    [Fact]
    public async Task PreviousMediaResult_CannotOverwriteCurrentMediaKeyframes()
    {
        using var keyframes = new TKeyframe();
        string previous = keyframes.TSourceCreate("previous.mp4", "previous source");
        string current = keyframes.TSourceCreate("current.mp4", "current source");
        keyframes.TKeyframeResultSet(previous, 5_000);
        keyframes.TKeyframeResultSet(current, 7_000, 27_000, 47_000);
        keyframes.TKeyframeScanSuspend(previous, honorCancellation: false);

        keyframes.TKeyframeStart(previous, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(1);
        keyframes.TKeyframeStart(current, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);
        keyframes.TKeyframeScanRelease(previous);
        await TKeyframe.TKeyframeSettleRun();

        Assert.Equal(new long[] { 7_000, 27_000, 47_000 }, keyframes.TKeyframeLatest!.TKeyframeList);
        Assert.DoesNotContain(5_000, keyframes.TKeyframeLatest.TKeyframeList);
    }

    [Fact]
    public async Task FailedScan_DoesNotBecomeCoverage_AndNavigationFailsAfterRetries()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("failing.mp4", "failing source");
        keyframes.TKeyframeResultSet(source, 25_000);
        keyframes.TKeyframeFailureSet(source, int.MaxValue);

        for (int attempt = 0; attempt < 3; attempt++)
        {
            keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
            await keyframes.TKeyframeScanRead(3 * (attempt + 1));
            await TKeyframe.TKeyframeSettleRun();
        }

        Assert.Empty(keyframes.TKeyframeLatest!.TKeyframeCoverage);
        Assert.Null(keyframes.TKeyframeCacheLoad(source, TimeSpan.FromSeconds(60)));
        LKeyframeMoveResult result = keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(30), 0);
        Assert.True(result.LKeyframeFailed);
        Assert.False(result.LKeyframeReady);

        int scans = keyframes.TKeyframeScanCount;
        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframe.TKeyframeSettleRun();
        Assert.Equal(scans, keyframes.TKeyframeScanCount);
    }

    [Fact]
    public async Task TransientScanFailure_IsRetriedOnNextRequest()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("transient.mp4", "transient source");
        keyframes.TKeyframeResultSet(source, 5_000, 25_000, 45_000);
        keyframes.TKeyframeFailureSet(source, 3);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(3);
        await TKeyframe.TKeyframeSettleRun();
        Assert.Empty(keyframes.TKeyframeLatest!.TKeyframeCoverage);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);

        Assert.Equal(new long[] { 5_000, 25_000, 45_000 }, keyframes.TKeyframeLatest!.TKeyframeList);
        Assert.True(keyframes.TKeyframeMoveRead(TimeSpan.FromSeconds(30), 0).LKeyframeReady);
    }

    [Fact]
    public async Task ReplacedSourceContent_WithSameDuration_RebuildsKeyframes()
    {
        using var keyframes = new TKeyframe();
        string source = keyframes.TSourceCreate("replaced.mp4", "first content");
        keyframes.TKeyframeResultSet(source, 5_000);

        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeCoverageRead(3);
        Assert.Equal(new long[] { 5_000 }, keyframes.TKeyframeLatest!.TKeyframeList);

        keyframes.TKeyframeSourceSet(source, "second content, longer");
        keyframes.TKeyframeResultSet(source, 7_000);
        keyframes.TKeyframeStart(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await TKeyframe.TKeyframeSettleRun();
        await keyframes.TKeyframeCoverageRead(3);

        Assert.Equal(new long[] { 7_000 }, keyframes.TKeyframeLatest!.TKeyframeList);
    }

    [Fact]
    public async Task MissingNewSource_CancelsPreviousScan_AndPublishesEmptyState()
    {
        using var keyframes = new TKeyframe();
        string previous = keyframes.TSourceCreate("stale.mp4", "stale source");
        string missing = keyframes.TSourceCreate("missing.mp4", "missing source");
        keyframes.TKeyframeResultSet(previous, 5_000);
        keyframes.TKeyframeScanSuspend(previous, honorCancellation: false);

        keyframes.TKeyframeStart(previous, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.TKeyframeScanRead(1);
        keyframes.TKeyframeSourceDelete(missing);
        keyframes.TKeyframeStart(missing, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        keyframes.TKeyframeScanRelease(previous);
        await TKeyframe.TKeyframeSettleRun();

        Assert.Empty(keyframes.TKeyframeLatest!.TKeyframeList);
        Assert.Empty(keyframes.TKeyframeLatest.TKeyframeCoverage);
        Assert.DoesNotContain(
            keyframes.TKeyframeNotices,
            notice => notice.TKeyframeSerial == keyframes.TKeyframeLatest.TKeyframeSerial
                && notice.TKeyframeList.Contains(5_000));
    }
}
