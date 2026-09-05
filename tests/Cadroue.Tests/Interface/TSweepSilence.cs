using System.Linq;

using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests;

public sealed class TSweepSilence
{
    [Fact]
    public void LSweepSilenceFormat_EmitsSilencedetectMapsAudioAndDisablesVideo()
    {
        string lArgs = LSweep.LSweepSilenceFormat("in.mp4", -30, 0.5);

        Assert.Contains("silencedetect=noise=-30dB:d=0.5", lArgs);
        Assert.Contains("0:a:0", lArgs);
        Assert.Contains("-vn", lArgs);
    }

    [Fact]
    public void LSweepSilenceParse_ReadsOneIntervalFromPair()
    {
        string[] lLines =
        {
            "[silencedetect @ x] silence_start: 12.5",
            "[silencedetect @ x] silence_end: 15.75 | silence_duration: 3.25"
        };

        (TimeSpan Start, TimeSpan End) lInterval =
            Assert.Single(LSweep.LSweepSilenceParse(lLines, TimeSpan.FromSeconds(20)));
        Assert.Equal(TimeSpan.FromSeconds(12.5), lInterval.Start);
        Assert.Equal(TimeSpan.FromSeconds(15.75), lInterval.End);
    }

    [Fact]
    public void LSweepSilenceParse_ClampsDanglingStartToDuration()
    {
        string[] lLines =
        {
            "[silencedetect @ x] silence_start: 12.5"
        };

        (TimeSpan Start, TimeSpan End) lInterval =
            Assert.Single(LSweep.LSweepSilenceParse(lLines, TimeSpan.FromSeconds(20)));
        Assert.Equal(TimeSpan.FromSeconds(12.5), lInterval.Start);
        Assert.Equal(TimeSpan.FromSeconds(20), lInterval.End);
    }

    [Fact]
    public void LSweepSilenceParse_ReturnsEmptyWithoutLines()
    {
        Assert.Empty(LSweep.LSweepSilenceParse(Array.Empty<string>(), TimeSpan.FromSeconds(20)));
    }

}
