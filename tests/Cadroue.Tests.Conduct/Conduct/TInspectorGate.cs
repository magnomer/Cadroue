using System;
using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorGate
{
    private readonly CInspector tInspectorGate = TAtelier.TAtelierCreate()
        .CAtelierInspectorCreate();

    [Fact]
    public void InspectorVideoAccept_Plan_SetsEveryVideoToolFromItsRead()
    {
        TInspectorPort plan = new()
        {
            TInspectorGamma = (true, [10, -20, 30, -40, 50]),
            TInspectorExposure = (true, 1.5),
            TInspectorVolume = (true, 6),
        };

        tInspectorGate.CInspectorVideo.CInspectorVideoAccept(plan);

        Assert.Equal(["tone", "whitebalance", "gamma", "exposure"], plan.TInspectorCalls);
        Assert.True(tInspectorGate.CInspectorGamma.CGammaActive);
        Assert.Equal([10d, -20d, 30d, -40d, 50d], tInspectorGate.CInspectorGamma.CGammaValues);
        Assert.True(tInspectorGate.CInspectorExposure.CExposureActive);
        Assert.Equal(1.5, tInspectorGate.CInspectorExposure.CExposureValue);
        Assert.False(tInspectorGate.CInspectorVolume.CVolumeActive);
        Assert.Equal([false, false, false], tInspectorGate.CInspectorTone.CToneActives);
    }

    [Fact]
    public void InspectorAudioAccept_Plan_SetsEveryAudioToolFromItsRead()
    {
        TInspectorPort plan = new()
        {
            TInspectorExposure = (true, 2),
            TInspectorVolume = (true, -6),
            TInspectorEqualizer = (true, [(100, 3), (2000, -4)], null),
        };

        tInspectorGate.CInspectorAudio.CInspectorAudioAccept(plan);

        Assert.Equal(
            ["highpass", "lowpass", "noise", "equalizer", "volume", "leveling", "skip"], plan.TInspectorCalls);
        Assert.True(tInspectorGate.CInspectorVolume.CVolumeActive);
        Assert.Equal(-6, tInspectorGate.CInspectorVolume.CVolumeGain);
        Assert.True(tInspectorGate.CInspectorEqualizer.CEqualizerActive);
        Assert.Equal([(100d, 3d), (2000d, -4d)], tInspectorGate.CInspectorEqualizer.CEqualizerBands);
        Assert.Null(tInspectorGate.CInspectorEqualizer.CEqualizerToken);
        Assert.False(tInspectorGate.CInspectorExposure.CExposureActive);
    }

    [Fact]
    public void InspectorAccept_NoPlan_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorVideo.CInspectorVideoAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorAudio.CInspectorAudioAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorEditAccept(null!));
    }

    [Fact]
    public void InspectorCapableSet_Facts_EachToolTakesItsOwn()
    {
        tInspectorGate.CInspectorCapableSet(eq: true, exposure: false, whitebalance: false, preview: true);

        Assert.True(tInspectorGate.CInspectorGamma.CGammaCapable);
        Assert.True(tInspectorGate.CInspectorGamma.CGammaPreview);
        Assert.False(tInspectorGate.CInspectorExposure.CExposureCapable);
        Assert.False(tInspectorGate.CInspectorExposure.CExposurePreview);
        Assert.True(tInspectorGate.CInspectorTone.CToneCapable);
        Assert.False(tInspectorGate.CInspectorWhitebalance.CWhitebalanceCapable);

        tInspectorGate.CInspectorCapableSet(eq: false, exposure: true, whitebalance: true, preview: true);

        Assert.True(tInspectorGate.CInspectorWhitebalance.CWhitebalanceCapable);
        Assert.True(tInspectorGate.CInspectorWhitebalance.CWhitebalancePreview);
        Assert.False(tInspectorGate.CInspectorGamma.CGammaCapable);
        Assert.False(tInspectorGate.CInspectorTone.CToneCapable);
        Assert.True(tInspectorGate.CInspectorExposure.CExposureCapable);
        Assert.True(tInspectorGate.CInspectorExposure.CExposurePreview);
    }
}
