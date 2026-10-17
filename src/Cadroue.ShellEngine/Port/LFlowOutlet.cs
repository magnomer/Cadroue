using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the flow gate: every member forwards to the same-named member of the engine's flow facade,
// and each per-flow facade it makes comes back wrapped in that flow's outlet. It rejects a null engine and keeps it for
// every forward.
public sealed class LFlowOutlet : LFlowPort
{
    private readonly LEngine _lFlowOutletEngine;

    public LFlowOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lFlowOutletEngine = lEngine;
    }

    public bool LFlowWaveformRead() => _lFlowOutletEngine.LEngineFlow.LFlowWaveformRead();

    public bool LFlowOrderRead() => _lFlowOutletEngine.LEngineFlow.LFlowOrderRead();

    public string LFlowWheelRead() => _lFlowOutletEngine.LEngineFlow.LFlowWheelRead();

    public bool LFlowDragRead() => _lFlowOutletEngine.LEngineFlow.LFlowDragRead();

    public void LFlowRangeZoom(LSpool lRange, TimeSpan lCursor, int lSteps) =>
        _lFlowOutletEngine.LEngineFlow.LFlowRangeZoom(lRange, lCursor, lSteps);

    public TimeSpan LFlowStepResolve(LSpool lRange, int lSteps) =>
        _lFlowOutletEngine.LEngineFlow.LFlowStepResolve(lRange, lSteps);

    public (string LFlowName, string LFlowPrefix, string LFlowSuffix)? LFlowNameRead(LSegment lSegment, int lIndex) =>
        _lFlowOutletEngine.LEngineFlow.LFlowNameRead(lSegment, lIndex);

    public (TimeSpan LFlowDuration, bool LFlowAudio, LSpool LFlowRange) LFlowMediaRead(LMediaInfo lMedia) =>
        _lFlowOutletEngine.LEngineFlow.LFlowMediaRead(lMedia);

    public LSectionPort LFlowSectionCreate(Func<string> lSource) =>
        new LSectionOutlet(_lFlowOutletEngine.LEngineFlow.LFlowSectionCreate(lSource));

    public LKeyframePort LFlowKeyframeCreate(Func<string> lSource) =>
        new LKeyframeOutlet(_lFlowOutletEngine.LEngineFlow.LFlowKeyframeCreate(lSource));

    public LWaveformPort LFlowWaveformCreate() =>
        new LWaveformOutlet(_lFlowOutletEngine.LEngineFlow.LFlowWaveformCreate());

    public double LFlowHeightRead() => _lFlowOutletEngine.LEngineFlow.LFlowHeightRead();
}
