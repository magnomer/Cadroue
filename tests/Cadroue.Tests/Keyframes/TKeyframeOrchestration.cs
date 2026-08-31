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
}
