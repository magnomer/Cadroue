using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The inspector's video plans: a video plan sets every video tool, and the video and carried plans are built from the
// tools' held values. CInspector makes one and holds the tools it reads and sets.
public sealed class CInspectorVideo
{
    private readonly LVideoPort _cInspectorVideoPort;
    private readonly CInspector _cInspectorVideoOwner;

    internal CInspectorVideo(LVideoPort port, CInspector owner)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(owner);
        _cInspectorVideoPort = port;
        _cInspectorVideoOwner = owner;
    }

    // A video plan sets every video tool, each from its read; a step the plan lacks reads as the Core default.
    public void CInspectorVideoAccept(LInspectorPort plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        CInspector owner = _cInspectorVideoOwner;
        (IReadOnlyList<bool> toneActives, IReadOnlyList<double> toneValues) = plan.LInspectorToneRead();
        owner.CInspectorTone.CToneStepSet(toneActives, toneValues);
        owner.CInspectorWhitebalance.CWhitebalanceStepSet(plan.LInspectorWhitebalanceRead());
        (bool gammaActive, IReadOnlyList<double> gammaValues) = plan.LInspectorGammaRead();
        owner.CInspectorGamma.CGammaStepSet(gammaActive, gammaValues);
        (bool exposureActive, double exposureValue) = plan.LInspectorExposureRead();
        owner.CInspectorExposure.CExposureStepSet(exposureActive, exposureValue);
    }

    // The video plan in the caller's order: a step from every video tool's held values for each processing step that
    // names one, built by the engine and answered as a plan read. Without the mpv-only filters, gamma, white balance
    // and exposure are left out.
    public LInspectorPort CInspectorVideoRead(IReadOnlyList<string> processing, bool mpvOnly)
    {
        ArgumentNullException.ThrowIfNull(processing);
        CInspector owner = _cInspectorVideoOwner;
        return _cInspectorVideoPort.LVideoPlanCreate(
            processing,
            CInspectorToneRead(0),
            CInspectorToneRead(1),
            CInspectorToneRead(2),
            mpvOnly ? (owner.CInspectorGamma.CGammaActive, owner.CInspectorGamma.CGammaValues) : null,
            mpvOnly ? owner.CInspectorWhitebalance.CWhitebalanceAnswer : null,
            mpvOnly ? (owner.CInspectorExposure.CExposureActive, owner.CInspectorExposure.CExposureValue) : null,
            (owner.CInspectorCurve.CCurveActive, owner.CInspectorCurve.CCurveChannels));
    }

    // The carried video plan: each video tool the user keeps, from its held values, in the tools' order, built by the
    // engine and answered as a plan read.
    public LInspectorPort CInspectorCarriedRead()
    {
        CInspector owner = _cInspectorVideoOwner;
        return _cInspectorVideoPort.LVideoPersistentCreate(
            owner.CInspectorTone.CTonePersistents[0] ? CInspectorToneRead(0) : null,
            owner.CInspectorTone.CTonePersistents[1] ? CInspectorToneRead(1) : null,
            owner.CInspectorTone.CTonePersistents[2] ? CInspectorToneRead(2) : null,
            owner.CInspectorGamma.CGammaPersistent
                ? (owner.CInspectorGamma.CGammaActive, owner.CInspectorGamma.CGammaValues)
                : null,
            owner.CInspectorWhitebalance.CWhitebalancePersistent
                ? owner.CInspectorWhitebalance.CWhitebalanceAnswer
                : null,
            owner.CInspectorExposure.CExposurePersistent
                ? (owner.CInspectorExposure.CExposureActive, owner.CInspectorExposure.CExposureValue)
                : null,
            owner.CInspectorCurve.CCurvePersistent
                ? (owner.CInspectorCurve.CCurveActive, owner.CInspectorCurve.CCurveChannels)
                : null);
    }

    // A tone slot's switch and value: 0 brightness, 1 contrast, 2 saturation.
    private (bool, double) CInspectorToneRead(int slot)
    {
        CTone tone = _cInspectorVideoOwner.CInspectorTone;
        return (tone.CToneActives[slot], tone.CToneValues[slot]);
    }
}
