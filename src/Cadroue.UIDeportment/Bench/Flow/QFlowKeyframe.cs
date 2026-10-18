using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The keyframes' wiring: relays the keyframe gate's timer, notice and change answers to the surface and hands it the
// timer ticks and the notice hop. The keyframes pass through here only for the readers not yet moved to a gate.
public sealed class QFlowKeyframe
{
    private readonly CKeyframe qFlowKeyframeGate;

    public QFlowKeyframe(CKeyframe lGate)
    {
        qFlowKeyframeGate = lGate;
        QFlowKeyframeAttach();
    }

    public event Action? QFlowTimerDeferred;
    public event Action<TimeSpan>? QFlowTimerResumed;
    public event Action? QFlowTimerStopped;
    public event Action? QFlowKeyframeReady;
    public event Action? QFlowKeyframeChanged;

    public bool QFlowResumePending => qFlowKeyframeGate.CKeyframeResumePending;

    public IReadOnlyList<LKeyframeEntry> QFlowKeyframeEntries => qFlowKeyframeGate.CKeyframeEntries;

    public IReadOnlyList<LKeyframeScanRange> QFlowKeyframeRanges => qFlowKeyframeGate.CKeyframeRanges;

    public static TimeSpan QFlowRequestDelay => TimeSpan.FromMilliseconds(250);

    public static TimeSpan QFlowResumeDelay =>
        TimeSpan.FromMilliseconds(LPreference.LPreferenceStateCurrent.LPreferenceKeyframeDelay);

    public void QFlowKeyframeDefer() => qFlowKeyframeGate.CKeyframeDefer();

    public void QFlowKeyframeSuspend(bool lResume) => qFlowKeyframeGate.CKeyframeSuspend(lResume);

    public void QFlowKeyframeTick() => qFlowKeyframeGate.CKeyframeRun();

    // Runs on the UI thread, once per notice the gate told of.
    public void QFlowKeyframeApply() => qFlowKeyframeGate.CKeyframeApply();

    private void QFlowKeyframeAttach()
    {
        qFlowKeyframeGate.CKeyframeDeferred += QFlowTimerDefer;
        qFlowKeyframeGate.CKeyframeResumed += QFlowTimerResume;
        qFlowKeyframeGate.CKeyframeStopped += QFlowTimerStop;
        qFlowKeyframeGate.CKeyframeArrived += QFlowKeyframeRaise;
        qFlowKeyframeGate.CKeyframeChanged += QFlowKeyframeChange;
    }

    private void QFlowTimerDefer() => QFlowTimerDeferred?.Invoke();

    private void QFlowTimerResume(TimeSpan lDelay) => QFlowTimerResumed?.Invoke(lDelay);

    private void QFlowTimerStop() => QFlowTimerStopped?.Invoke();

    private void QFlowKeyframeRaise() => QFlowKeyframeReady?.Invoke();

    private void QFlowKeyframeChange() => QFlowKeyframeChanged?.Invoke();
}
