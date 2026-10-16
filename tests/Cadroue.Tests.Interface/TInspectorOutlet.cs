using Cadroue.Core;
using Cadroue.ShellEngine;

using Xunit;

namespace Cadroue.Tests.Interface;

public sealed class TInspectorOutlet
{
    [Fact]
    public void InspectorGammaRead_StepPresent_ReadsItsSwitchAndSlots()
    {
        LInspectorOutlet outlet = new(new LWorkVideo(
            [LWorkVideoStep.LWorkGammaCreate(true, 10, -20, 30, -40, 50)]));

        (bool active, IReadOnlyList<double> values) = outlet.LInspectorGammaRead();

        Assert.True(active);
        Assert.Equal([10d, -20d, 30d, -40d, 50d], values);
    }

    [Fact]
    public void InspectorGammaRead_StepMissing_ReadsTheCoreDefault()
    {
        LInspectorOutlet outlet = new(LWorkVideo.LWorkVideoCreate());

        (bool active, IReadOnlyList<double> values) = outlet.LInspectorGammaRead();

        Assert.False(active);
        Assert.Equal(LWorkVideoStep.LWorkGammaCreate(false, 0).LWorkGammaRead().LWorkSlotRead(), values);
    }

    [Fact]
    public void InspectorExposureRead_StepPresent_ReadsItsSwitchAndValue()
    {
        LInspectorOutlet outlet = new(new LWorkVideo([LWorkVideoStep.LWorkExposureCreate(true, 1.5)]));

        Assert.Equal((true, 1.5), outlet.LInspectorExposureRead());
    }

    [Fact]
    public void InspectorExposureRead_StepMissing_ReadsTheCoreDefault()
    {
        LInspectorOutlet outlet = new(LWorkVideo.LWorkVideoCreate());
        LWorkVideoStep standard = LWorkVideoStep.LWorkExposureCreate(false, 0);

        Assert.Equal((standard.LWorkStepActive, standard.LWorkStepValue), outlet.LInspectorExposureRead());
    }

    [Fact]
    public void InspectorVolumeRead_StepPresent_ReadsItsSwitchAndGain()
    {
        LInspectorOutlet outlet = new(new LWorkAudio([LWorkAudioStep.LWorkVolumeCreate(true, -6)]));

        Assert.Equal((true, -6d), outlet.LInspectorVolumeRead());
    }

    [Fact]
    public void InspectorVolumeRead_StepMissing_ReadsTheCoreDefault()
    {
        LInspectorOutlet outlet = new(new LWorkAudio([]));
        var standard = (LWorkVolumeStep)LWorkAudioStep.LWorkVolumeCreate(false, 0);

        Assert.Equal((standard.LWorkStepActive, standard.LWorkVolumeGain), outlet.LInspectorVolumeRead());
    }

    [Fact]
    public void InspectorRead_OtherMedium_ReadsTheCoreDefaults()
    {
        LInspectorOutlet video = new(new LWorkVideo([LWorkVideoStep.LWorkExposureCreate(true, 2)]));
        LInspectorOutlet audio = new(new LWorkAudio([LWorkAudioStep.LWorkVolumeCreate(true, 3)]));

        Assert.Equal((false, 0d), video.LInspectorVolumeRead());
        Assert.Equal((false, 0d), audio.LInspectorExposureRead());
        Assert.False(audio.LInspectorGammaRead().LInspectorActive);
    }

    [Fact]
    public void InspectorOutlet_NoPlan_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new LInspectorOutlet((LWorkVideo)null!));
        Assert.Throws<ArgumentNullException>(() => new LInspectorOutlet((LWorkAudio)null!));
    }
}
