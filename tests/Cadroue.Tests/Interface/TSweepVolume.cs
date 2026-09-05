using System.Linq;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSweepVolume
{
    [Fact]
    public void LSweepVolumeFormat_LufsMapsAudioUsesEbur128AndDisablesVideo()
    {
        string lArgs = LSweep.LSweepVolumeFormat("in.mp4", LDetectorMetricMode.LDetectorMetricLufs);

        Assert.Contains("ebur128=metadata=1,ametadata=print:key=lavfi.r128.M", lArgs);
        Assert.Contains("0:a:0", lArgs);
        Assert.Contains("-vn", lArgs);
        Assert.DoesNotContain("astats", lArgs);
    }

    [Fact]
    public void LSweepVolumeFormat_RmsUsesAstatsRmsLevel()
    {
        string lArgs = LSweep.LSweepVolumeFormat("in.mp4", LDetectorMetricMode.LDetectorMetricRms);

        Assert.Contains("astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level", lArgs);
        Assert.DoesNotContain("ebur128", lArgs);
    }

    [Fact]
    public void LSweepVolumeParse_PairsTimeWithLufsOrRmsLoudness()
    {
        string[] lLines =
        {
            "lavfi.r128.M=-23.000000",
            "frame:0    pts_time:0.000000",
            "lavfi.r128.M=-18.000000",
            "frame:1    pts_time:0.500000",
            "lavfi.astats.Overall.RMS_level=-30.000000"
        };

        IReadOnlyList<LSweepSample> lSamples = LSweep.LSweepVolumeParse(lLines);

        Assert.Equal(2, lSamples.Count);
        Assert.Equal(TimeSpan.Zero, lSamples[0].LSweepSampleTime);
        Assert.Equal(-18.0, lSamples[0].LSweepSampleLuma);
        Assert.Equal(TimeSpan.FromSeconds(0.5), lSamples[1].LSweepSampleTime);
        Assert.Equal(-30.0, lSamples[1].LSweepSampleLuma);
    }

    [Fact]
    public void LSweepVolumeParse_ReturnsEmptyWithoutLines()
    {
        Assert.Empty(LSweep.LSweepVolumeParse(Array.Empty<string>()));
    }

    [Fact]
    public void LSweepVolumeParse_SkipsSilentGateInfinity()
    {
        string[] lLines =
        {
            "frame:0    pts_time:0.000000",
            "lavfi.astats.Overall.RMS_level=-inf",
            "frame:1    pts_time:0.500000",
            "lavfi.astats.Overall.RMS_level=-20.000000"
        };

        IReadOnlyList<LSweepSample> lSamples = LSweep.LSweepVolumeParse(lLines);

        LSweepSample lSample = Assert.Single(lSamples);
        Assert.Equal(TimeSpan.FromSeconds(0.5), lSample.LSweepSampleTime);
        Assert.Equal(-20.0, lSample.LSweepSampleLuma);
    }

    [Fact]
    public void LSweepVolumeParse_SkipsNumericGateFloorBelowSilence()
    {
        string[] lLines =
        {
            "frame:0    pts_time:0.000000",
            "lavfi.r128.M=-120.691000",
            "frame:1    pts_time:0.100000",
            "lavfi.r128.M=-18.000000"
        };

        IReadOnlyList<LSweepSample> lSamples = LSweep.LSweepVolumeParse(lLines);

        LSweepSample lSample = Assert.Single(lSamples);
        Assert.Equal(TimeSpan.FromSeconds(0.1), lSample.LSweepSampleTime);
        Assert.Equal(-18.0, lSample.LSweepSampleLuma);
    }
}
