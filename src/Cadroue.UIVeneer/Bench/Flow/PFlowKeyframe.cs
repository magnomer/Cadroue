using System.Windows.Threading;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PFlowKeyframe
{
    private readonly QFlowKeyframe lKeyframe;
    private readonly Dispatcher pKeyframeDispatcher;
    private readonly DispatcherTimer pKeyframeRequestTimer = new() { Interval = QFlowKeyframe.QFlowRequestDelay };
    private readonly DispatcherTimer pKeyframeResumeTimer = new() { Interval = QFlowKeyframe.QFlowResumeDelay };

    public PFlowKeyframe(QFlowKeyframe lKeyframeOwner, Dispatcher pDispatcher)
    {
        lKeyframe = lKeyframeOwner;
        pKeyframeDispatcher = pDispatcher;
        PFlowKeyframeAttach();
    }

    private void PFlowKeyframeAttach()
    {
        lKeyframe.QFlowTimerDeferred += PFlowTimerDefer;
        lKeyframe.QFlowTimerResumed += PFlowTimerResume;
        lKeyframe.QFlowTimerStopped += PFlowTimerReset;
        lKeyframe.QFlowKeyframeReady += PFlowKeyframeHandle;
        pKeyframeRequestTimer.Tick += PFlowTimerHandle;
        pKeyframeResumeTimer.Tick += PFlowTimerHandle;
    }

    public void PFlowKeyframeClose()
    {
        PFlowTimerReset();
        PFlowTimerDetach();
    }

    private void PFlowTimerDetach()
    {
        pKeyframeRequestTimer.Tick -= PFlowTimerHandle;
        pKeyframeResumeTimer.Tick -= PFlowTimerHandle;
    }

    private void PFlowTimerDefer()
    {
        pKeyframeRequestTimer.Stop();
        pKeyframeRequestTimer.Start();
    }

    private void PFlowTimerResume(TimeSpan lDelay)
    {
        pKeyframeResumeTimer.Interval = lDelay;
        pKeyframeResumeTimer.Start();
    }

    private void PFlowTimerReset()
    {
        pKeyframeRequestTimer.Stop();
        pKeyframeResumeTimer.Stop();
    }

    private void PFlowTimerHandle(object? sender, EventArgs e) => lKeyframe.QFlowKeyframeTick();

    private void PFlowKeyframeHandle() =>
        pKeyframeDispatcher.InvokeAsync(lKeyframe.QFlowKeyframeApply, DispatcherPriority.Background);
}
