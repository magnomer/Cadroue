using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkAudioTests
{
    [Fact]
    public void Format_MixedActivePlan_ProducesOrderedGraph()
    {
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkVolumeCreate(true, 4.5),
            TInterface.WorkHighCreate(true, 120, 1, 2, 0.7),
            TInterface.WorkEqualizerCreate(true, new[]
            {
                TInterface.WorkBandCreate(1000, 3),
                TInterface.WorkBandCreate(4000, -2)
            }),
            TInterface.WorkNormalizeCreate(true, LLeveling.LLevelingLoudness, -16, -1.5, 8, false, 300, 21, 10, 6)
        });

        Assert.Equal(
            "volume=4.5dB," +
            "highpass=f=120:poles=2:width_type=q:width=0.7," +
            "equalizer=f=1000:t=q:w=1:g=3," +
            "equalizer=f=4000:t=q:w=1:g=-2," +
            "loudnorm=I=-16:TP=-1.5:LRA=8",
            TInterface.WorkAudioFormat(audio));
    }

    [Fact]
    public void Format_InactiveSteps_AreOmitted()
    {
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkVolumeCreate(false, 4.5),
            TInterface.WorkLowCreate(true, 8000, 1, 1, 0.5)
        });

        Assert.Equal("lowpass=f=8000:poles=1:width_type=q:width=0.5", TInterface.WorkAudioFormat(audio));
    }

    [Fact]
    public void Format_TwoStageHighPass_RepeatsFragmentTwice()
    {
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkHighCreate(true, 120, 2, 2, 0.7)
        });

        Assert.Equal(
            "highpass=f=120:poles=2:width_type=q:width=0.7," +
            "highpass=f=120:poles=2:width_type=q:width=0.7",
            TInterface.WorkAudioFormat(audio));
    }

    [Fact]
    public void Format_ZeroGainBands_DropOut()
    {
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkEqualizerCreate(true, new[]
            {
                TInterface.WorkBandCreate(1000, 3),
                TInterface.WorkBandCreate(250, 0),
                TInterface.WorkBandCreate(4000, -2)
            })
        });

        Assert.Equal(
            "equalizer=f=1000:t=q:w=1:g=3,equalizer=f=4000:t=q:w=1:g=-2",
            TInterface.WorkAudioFormat(audio));
    }

    [Fact]
    public void Format_AllInactivePlan_YieldsEmpty()
    {
        LWorkAudio audio = TInterface.WorkAudioCreate(new[]
        {
            TInterface.WorkVolumeCreate(false, 4.5),
            TInterface.WorkLowCreate(false, 8000, 1, 1, 0.5)
        });

        Assert.Equal(string.Empty, TInterface.WorkAudioFormat(audio));
    }
}
