using Cadroue.Media;
using Xunit;

namespace Cadroue.Tests;

[Collection("MediaProbe")]
public sealed class TScoutMeasure
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
            return TScoutVideoRead("1");
        });

        probe.TScoutStart("cancelled.mp4", cancellation.Token);
        Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        cancellation.Cancel();
        release.Set();
        probe.TScoutIdleRead();

        Assert.Empty(probe.TScoutResultsRead());
        Assert.Equal(0, LMediaProbe.LMediaProbeCount);
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
                return TScoutVideoRead("1");
            }

            return TScoutVideoRead("2");
        });

        probe.TScoutStart("same.mp4");
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(5)));
        probe.TScoutStart("same.mp4");
        Assert.Single(probe.TScoutCountRead(1));
        releaseFirst.Set();
        probe.TScoutIdleRead();

        var result = Assert.Single(probe.TScoutResultsRead());
        Assert.Equal(TimeSpan.FromSeconds(2), result.LMediaProbeInfo!.LMediaInfoDuration);
        Assert.Equal(0, LMediaProbe.LMediaProbeCount);
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
                return TScoutVideoRead("3");
            }

            return TScoutVideoRead("4");
        });

        probe.TScoutStart("first.mp4");
        Assert.True(firstStarted.Wait(TimeSpan.FromSeconds(5)));
        probe.TScoutStart("second.mp4");
        Assert.Single(probe.TScoutCountRead(1));
        releaseFirst.Set();
        probe.TScoutIdleRead();

        var results = probe.TScoutResultsRead().OrderBy(result => result.LMediaProbePath).ToArray();
        Assert.Equal(2, results.Length);
        Assert.Equal("first.mp4", results[0].LMediaProbePath);
        Assert.Equal(TimeSpan.FromSeconds(3), results[0].LMediaProbeInfo!.LMediaInfoDuration);
        Assert.Equal("second.mp4", results[1].LMediaProbePath);
        Assert.Equal(TimeSpan.FromSeconds(4), results[1].LMediaProbeInfo!.LMediaInfoDuration);
        Assert.Equal(0, LMediaProbe.LMediaProbeCount);
    }

    private static string TScoutVideoRead(string duration) => TMediaProbe.TMediaProbeRead(
        duration,
        """{"codec_type":"video","codec_name":"h264","width":320,"height":180,"r_frame_rate":"30/1"}""");
}
