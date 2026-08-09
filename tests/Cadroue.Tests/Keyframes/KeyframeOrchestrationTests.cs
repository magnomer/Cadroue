using Xunit;

namespace Cadroue.Tests;

[Collection("Sidecar")]
public sealed class KeyframeOrchestrationTests
{
    [Fact]
    public async Task CancelledScanning_DoesNotPublishSuccessfulCompleteResult()
    {
        using var keyframes = new TKeyframes();
        string source = keyframes.SourceCreate("cancel.mp4", "cancel source");
        keyframes.ScanResultsSet(source, 5_000, 25_000, 45_000);
        keyframes.ScanBlock(source, honorCancellation: true);

        keyframes.Start(source, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForScanCountAsync(1);
        keyframes.Suspend();
        keyframes.ScanRelease(source);
        await TKeyframes.SettleAsync();

        Assert.DoesNotContain(keyframes.Notices, notice => notice.Coverage.Count == 3);
    }

    [Fact]
    public async Task PreviousMediaResult_CannotOverwriteCurrentMediaKeyframes()
    {
        using var keyframes = new TKeyframes();
        string previous = keyframes.SourceCreate("previous.mp4", "previous source");
        string current = keyframes.SourceCreate("current.mp4", "current source");
        keyframes.ScanResultsSet(previous, 5_000);
        keyframes.ScanResultsSet(current, 7_000, 27_000, 47_000);
        keyframes.ScanBlock(previous, honorCancellation: false);

        keyframes.Start(previous, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForScanCountAsync(1);
        keyframes.Start(current, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
        await keyframes.WaitForCoverageCountAsync(3);
        keyframes.ScanRelease(previous);
        await TKeyframes.SettleAsync();

        Assert.Equal(new long[] { 7_000, 27_000, 47_000 }, keyframes.Latest!.Keyframes);
        Assert.DoesNotContain(5_000, keyframes.Latest.Keyframes);
    }
}
