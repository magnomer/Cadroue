using System.Linq;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSweepLuminance
{
    [Fact]
    public void LSweepLuminanceFormat_FullKeepsEveryFrameFullResolution()
    {
        string lArgs = LSweep.LSweepLuminanceFormat("clip.mp4", LDetectorLuminanceMode.LDetectorLuminanceFull);

        Assert.DoesNotContain("scale=", lArgs);
        Assert.DoesNotContain("-skip_frame", lArgs);
        Assert.Contains("signalstats", lArgs);
    }

    [Fact]
    public void LSweepLuminanceFormat_NormalDownscalesButDecodesEveryFrame()
    {
        string lArgs = LSweep.LSweepLuminanceFormat("clip.mp4", LDetectorLuminanceMode.LDetectorLuminanceNormal);

        Assert.Contains("scale=", lArgs);
        Assert.DoesNotContain("-skip_frame", lArgs);
    }

    [Fact]
    public void LSweepLuminanceFormat_FastDownscalesAndSkipsToKeyframes()
    {
        string lArgs = LSweep.LSweepLuminanceFormat("clip.mp4", LDetectorLuminanceMode.LDetectorLuminanceFast);

        Assert.Contains("scale=", lArgs);
        Assert.Contains("-skip_frame nokey", lArgs);
    }

    [Fact]
    public void LSweepLuminanceParse_PairsTimeWithFollowingLuma()
    {
        string[] lLines =
        {
            "lavfi.signalstats.YAVG=99.000000",
            "garbage line without tokens",
            "frame:0    pts_time:0.000000",
            "lavfi.signalstats.YAVG=158.000000",
            "frame:1    pts_time:0.500000",
            "lavfi.signalstats.YAVG=122.000000",
            "frame:2    pts_time:1.000000"
        };

        IReadOnlyList<LSweepSample> lSamples = LSweep.LSweepLuminanceParse(lLines);

        Assert.Equal(2, lSamples.Count);
        Assert.Equal(TimeSpan.Zero, lSamples[0].LSweepSampleTime);
        Assert.Equal(158.0, lSamples[0].LSweepSampleLuma);
        Assert.Equal(TimeSpan.FromSeconds(0.5), lSamples[1].LSweepSampleTime);
        Assert.Equal(122.0, lSamples[1].LSweepSampleLuma);
    }

    [Fact]
    public void LSweepLuminanceResolve_FlatSeriesYieldsNoBoundaryEvenAtZero()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), 158.0));
        }

        Assert.Empty(LSweep.LSweepLuminanceResolve(lSamples, 0.5, 0));
    }

    [Fact]
    public void LSweepLuminanceResolve_SustainedStepYieldsOneBoundary()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            double lLuma = lIndex < 10 ? 158.0 : 122.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), lLuma));
        }

        IReadOnlyList<TimeSpan> lBoundaries = LSweep.LSweepLuminanceResolve(lSamples, 0.5, 10);

        TimeSpan lBoundary = Assert.Single(lBoundaries);
        Assert.Equal(TimeSpan.FromSeconds(1.0), lBoundary);
    }

    [Fact]
    public void LSweepLuminanceResolve_SubThresholdStepYieldsNoBoundary()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            double lLuma = lIndex < 10 ? 158.0 : 150.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), lLuma));
        }

        Assert.Empty(LSweep.LSweepLuminanceResolve(lSamples, 0.5, 10));
    }

    [Fact]
    public void LSweepLuminanceResolve_LargeInputResolvesToSingleStep()
    {
        const int lHalf = 20000;
        var lSamples = new List<LSweepSample>(lHalf * 2);
        for (int lIndex = 0; lIndex < lHalf * 2; lIndex++)
        {
            double lLuma = lIndex < lHalf ? 100.0 : 200.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.04), lLuma));
        }

        IReadOnlyList<TimeSpan> lBoundaries = LSweep.LSweepLuminanceResolve(lSamples, 0.5, 10);

        TimeSpan lBoundary = Assert.Single(lBoundaries);
        Assert.Equal(lSamples[lHalf].LSweepSampleTime, lBoundary);
    }

    [Fact]
    public void LSweepMinimumResolve_DropsBoundariesUnderGap()
    {
        var lBoundaries = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1.4),
            TimeSpan.FromSeconds(3),
            TimeSpan.FromSeconds(3.2)
        };

        IReadOnlyList<TimeSpan> lKept = LSweep.LSweepMinimumResolve(lBoundaries, 0.5);

        Assert.Equal(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3) }, lKept);
    }

    [Fact]
    public void LSweepMinimumResolve_ZeroGapKeepsAll()
    {
        var lBoundaries = new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.1) };

        Assert.Equal(lBoundaries, LSweep.LSweepMinimumResolve(lBoundaries, 0));
    }

    [Fact]
    public void LSweepBoundaryResolve_LevelJumpYieldsOneBoundaryOnRawDelta()
    {
        var lSamples = new List<LSweepSample>();
        for (int lIndex = 0; lIndex < 20; lIndex++)
        {
            double lLevel = lIndex < 10 ? -30.0 : -12.0;
            lSamples.Add(new LSweepSample(TimeSpan.FromSeconds(lIndex * 0.1), lLevel));
        }

        IReadOnlyList<TimeSpan> lBoundaries = LSweep.LSweepBoundaryResolve(lSamples, 0.5, 6);

        TimeSpan lBoundary = Assert.Single(lBoundaries);
        Assert.Equal(TimeSpan.FromSeconds(1.0), lBoundary);
    }

    [Fact]
    public void LSweepBoundaryResolve_EmptyInputYieldsNoBoundary()
    {
        Assert.Empty(LSweep.LSweepBoundaryResolve(Array.Empty<LSweepSample>(), 0.5, 6));
    }
}
