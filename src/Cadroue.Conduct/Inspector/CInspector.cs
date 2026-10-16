using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The inspector's gate: one decision object per tool, each exposed as one property. CAtelier makes one per inspector.
// Every decision that spans its tools is one member here, so an owner asks once whatever the number of tools.
public sealed class CInspector
{
    internal CInspector()
    {
    }

    public CGamma CInspectorGamma { get; } = new();

    public CExposure CInspectorExposure { get; } = new();

    public CVolume CInspectorVolume { get; } = new();

    // The owner reports which filters the editor has and whether the preview runs on mpv; each tool takes its fact.
    public void CInspectorCapableSet(bool eq, bool exposure, bool preview)
    {
        CInspectorExposure.CExposureCapableSet(exposure, preview);
        CInspectorGamma.CGammaCapableSet(eq, preview);
    }

    // A video plan sets every video tool, each from its read; a step the plan lacks reads as the Core default.
    public void CInspectorVideoAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        (bool gammaActive, IReadOnlyList<double> gammaValues) = plan.LInspectorGammaRead();
        CInspectorGamma.CGammaStepSet(gammaActive, gammaValues);
        (bool exposureActive, double exposureValue) = plan.LInspectorExposureRead();
        CInspectorExposure.CExposureStepSet(exposureActive, exposureValue);
    }

    // An audio plan sets every audio tool, each from its read; a step the plan lacks reads as the Core default.
    public void CInspectorAudioAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        (bool volumeActive, double volumeGain) = plan.LInspectorVolumeRead();
        CInspectorVolume.CVolumeStepSet(volumeActive, volumeGain);
    }
}
