using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class LWorkAudioTests
{
    [Fact]
    public void Format_MixedActivePlan_ProducesOrderedGraph()
    {
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkVolumeCreate(true, 4.5),
            TInterface.TWorkHighCreate(true, 120, 1, 2, 0.7),
            TInterface.TWorkEqualizerCreate(true, new[]
            {
                TInterface.TWorkBandCreate(1000, 3),
                TInterface.TWorkBandCreate(4000, -2)
            }),
            TInterface.TWorkNormalizeCreate(true, LLeveling.LLevelingLoudness, -16, -1.5, 8, false, 300, 21, 10, 6)
        });

        Assert.Equal(
            "volume=4.5dB," +
            "highpass=f=120:poles=2:width_type=q:width=0.7," +
            "equalizer=f=1000:t=q:w=1:g=3," +
            "equalizer=f=4000:t=q:w=1:g=-2," +
            "loudnorm=I=-16:TP=-1.5:LRA=8",
            TInterface.TWorkAudioFormat(audio));
    }

    [Fact]
    public void AudioPlanResolve_PersistentSkipOff_OverridesFileSkipOn()
    {
        LWorkAudio file = TInterface.TWorkAudioCreate(new List<LWorkAudioStep>(), true);
        LWorkAudio persistent = TInterface.TWorkAudioCreate(new List<LWorkAudioStep>(), true);

        LWorkAudio resolved = TInterface.TAudioPlanResolve(file, persistent, skipPersistent: true, skipApply: false);

        Assert.False(resolved.LWorkAudioSkip);
    }

    [Fact]
    public void AudioPlanResolve_SkipNotPersistent_KeepsFileSkip()
    {
        LWorkAudio file = TInterface.TWorkAudioCreate(new List<LWorkAudioStep>(), true);
        LWorkAudio persistent = TInterface.TWorkAudioCreate(new List<LWorkAudioStep>(), false);

        LWorkAudio resolved = TInterface.TAudioPlanResolve(file, persistent, skipPersistent: false, skipApply: false);

        Assert.True(resolved.LWorkAudioSkip);
    }

    [Fact]
    public void Format_InactiveSteps_AreOmitted()
    {
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkVolumeCreate(false, 4.5),
            TInterface.TWorkLowCreate(true, 8000, 1, 1, 0.5)
        });

        Assert.Equal("lowpass=f=8000:poles=1:width_type=q:width=0.5", TInterface.TWorkAudioFormat(audio));
    }

    [Fact]
    public void Format_TwoStageHighPass_RepeatsFragmentTwice()
    {
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkHighCreate(true, 120, 2, 2, 0.7)
        });

        Assert.Equal(
            "highpass=f=120:poles=2:width_type=q:width=0.7," +
            "highpass=f=120:poles=2:width_type=q:width=0.7",
            TInterface.TWorkAudioFormat(audio));
    }

    [Fact]
    public void Format_ZeroGainBands_DropOut()
    {
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkEqualizerCreate(true, new[]
            {
                TInterface.TWorkBandCreate(1000, 3),
                TInterface.TWorkBandCreate(250, 0),
                TInterface.TWorkBandCreate(4000, -2)
            })
        });

        Assert.Equal(
            "equalizer=f=1000:t=q:w=1:g=3,equalizer=f=4000:t=q:w=1:g=-2",
            TInterface.TWorkAudioFormat(audio));
    }

    [Fact]
    public void Format_AllInactivePlan_YieldsEmpty()
    {
        LWorkAudio audio = TInterface.TWorkAudioCreate(new[]
        {
            TInterface.TWorkVolumeCreate(false, 4.5),
            TInterface.TWorkLowCreate(false, 8000, 1, 1, 0.5)
        });

        Assert.Equal(string.Empty, TInterface.TWorkAudioFormat(audio));
    }
}
