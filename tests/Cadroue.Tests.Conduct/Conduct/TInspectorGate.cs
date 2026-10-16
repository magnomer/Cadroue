using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TInspectorGate
{
    private readonly CInspector tInspectorGate = new CAtelier(
            new TUsherPort(), new TLineagePort(), new TWorkspacePort(), new TPlacementPort(), new TViewerPort(),
            new TEditPort())
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

        tInspectorGate.CInspectorVideoAccept(plan);

        Assert.Equal(["gamma", "exposure"], plan.TInspectorCalls);
        Assert.True(tInspectorGate.CInspectorGamma.CGammaActive);
        Assert.Equal([10d, -20d, 30d, -40d, 50d], tInspectorGate.CInspectorGamma.CGammaValues);
        Assert.True(tInspectorGate.CInspectorExposure.CExposureActive);
        Assert.Equal(1.5, tInspectorGate.CInspectorExposure.CExposureValue);
        Assert.False(tInspectorGate.CInspectorVolume.CVolumeActive);
    }

    [Fact]
    public void InspectorAudioAccept_Plan_SetsEveryAudioToolFromItsRead()
    {
        TInspectorPort plan = new() { TInspectorExposure = (true, 2), TInspectorVolume = (true, -6) };

        tInspectorGate.CInspectorAudioAccept(plan);

        Assert.Equal(["volume"], plan.TInspectorCalls);
        Assert.True(tInspectorGate.CInspectorVolume.CVolumeActive);
        Assert.Equal(-6, tInspectorGate.CInspectorVolume.CVolumeGain);
        Assert.False(tInspectorGate.CInspectorExposure.CExposureActive);
    }

    [Fact]
    public void InspectorAccept_NoPlan_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorVideoAccept(null!));
        Assert.Throws<ArgumentNullException>(() => tInspectorGate.CInspectorAudioAccept(null!));
    }

    [Fact]
    public void InspectorCapableSet_Facts_EachToolTakesItsOwn()
    {
        tInspectorGate.CInspectorCapableSet(eq: true, exposure: false, preview: true);

        Assert.True(tInspectorGate.CInspectorGamma.CGammaCapable);
        Assert.True(tInspectorGate.CInspectorGamma.CGammaPreview);
        Assert.False(tInspectorGate.CInspectorExposure.CExposureCapable);
        Assert.False(tInspectorGate.CInspectorExposure.CExposurePreview);

        tInspectorGate.CInspectorCapableSet(eq: false, exposure: true, preview: true);

        Assert.False(tInspectorGate.CInspectorGamma.CGammaCapable);
        Assert.True(tInspectorGate.CInspectorExposure.CExposureCapable);
        Assert.True(tInspectorGate.CInspectorExposure.CExposurePreview);
    }
}
