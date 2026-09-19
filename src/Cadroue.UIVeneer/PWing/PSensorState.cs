using Cadroue.Core;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PInspector
{
    public LDetectorStillMode PSensorModeRead(LDetectorKind pDetectorKind) => LSensor.LSensorMode;

    public void PSensorModeApply(LDetectorKind pDetectorKind, LDetectorStillMode pDetectorMode) =>
        LSensor.LSensorModeSet(pDetectorMode);

    public LDetectorLuminanceMode PSensorSpeedRead(LDetectorKind pDetectorKind) => LSensor.LSensorSpeed;

    public void PSensorSpeedApply(LDetectorKind pDetectorKind, LDetectorLuminanceMode pDetectorMode) =>
        LSensor.LSensorSpeedSet(pDetectorMode);

    public LDetectorMetricMode PSensorMetricRead(LDetectorKind pDetectorKind) => LSensor.LSensorMetric;

    public void PSensorMetricApply(LDetectorKind pDetectorKind, LDetectorMetricMode pDetectorMode) =>
        LSensor.LSensorMetricSet(pDetectorMode);

    public LDetectorStep PSensorStepRead(LDetectorKind pDetectorKind) =>
        pDetectorKind == LDetectorKind.LDetectorKindBlank
            ? LDetector.LDetectorCreate(pDetectorKind) with { LDetectorStepEnabled = LBlank.LBlankStep.LDetectorBlankEnabled }
            : LSensor.LSensorStepRead(pDetectorKind);

    public void PSensorApply(LDetectorStep pDetectorStep) => LSensor.LSensorStepSet(pDetectorStep);

    public string PSensorPresetRead(LDetectorKind pDetectorKind) =>
        LSensor.LSensorTokenRead(pDetectorKind) ?? string.Empty;

    public void PSensorPresetApply(LDetectorKind pDetectorKind, string pToken) =>
        LSensor.LSensorTokenSet(pDetectorKind, pToken);
}
