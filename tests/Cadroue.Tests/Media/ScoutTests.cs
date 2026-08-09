using Xunit;

namespace Cadroue.Tests;

[Collection("MediaProbe")]
public sealed class ScoutTests
{
    [Fact]
    public void CancelledProbe_DoesNotPublishSuccessfulStaleResult()
    {
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using var cancellation = new CancellationTokenSource();
        using var probe = new TScoutProbe((_, _) =>
        {
            started.Set();
            release.Wait();
            return VideoOutput("1");
        });

        probe.Start("cancelled.mp4", cancellation.Token);
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        cancellation.Cancel();
        release.Set();
        probe.WaitForIdle();

        Assert.Empty(probe.ResultsRead());
    }

    [Fact]
    public void LaterProbe_CannotBeOverwrittenByObsoleteCompletion()
    {
        using var firstStarted = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        int call = 0;
        using var probe = new TScoutProbe((_, _) =>
        {
            if (Interlocked.Increment(ref call) == 1)
            {
                firstStarted.Set();
                releaseFirst.Wait();
                return VideoOutput("1");
            }

            return VideoOutput("2");
        });

        probe.Start("same.mp4");
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(5)));
        probe.Start("same.mp4");
        Assert.Single(probe.WaitForCount(1));
        releaseFirst.Set();
        probe.WaitForIdle();

        var result = Assert.Single(probe.ResultsRead());
        Assert.Equal(TimeSpan.FromSeconds(2), result.LMediaProbeInfo!.LMediaInfoDuration);
    }

    [Fact]
    public void IndependentProbes_KeepSourceIdentitiesSeparate()
    {
        using var firstStarted = new ManualResetEventSlim();
        using var releaseFirst = new ManualResetEventSlim();
        using var probe = new TScoutProbe((path, _) =>
        {
            if (path == "first.mp4")
            {
                firstStarted.Set();
                releaseFirst.Wait();
                return VideoOutput("3");
            }

            return VideoOutput("4");
        });

        probe.Start("first.mp4");
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(5)));
        probe.Start("second.mp4");
        Assert.Single(probe.WaitForCount(1));
        releaseFirst.Set();
        probe.WaitForIdle();

        var results = probe.ResultsRead().OrderBy(result => result.LMediaProbePath).ToArray();
        Assert.Equal(2, results.Length);
        Assert.Equal("first.mp4", results[0].LMediaProbePath);
        Assert.Equal(TimeSpan.FromSeconds(3), results[0].LMediaProbeInfo!.LMediaInfoDuration);
        Assert.Equal("second.mp4", results[1].LMediaProbePath);
        Assert.Equal(TimeSpan.FromSeconds(4), results[1].LMediaProbeInfo!.LMediaInfoDuration);
    }

    private static string VideoOutput(string duration) => MediaProbeTests.ProbeOutput(
        duration,
        """{"codec_type":"video","codec_name":"h264","width":320,"height":180,"r_frame_rate":"30/1"}""");
}
