using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

// The tab's close in order: its notices, its deportment, its surface, then its flow and viewer; a failed step is
// recorded and the next one still runs.
public sealed class QWorkspaceTeardown
{
    private static readonly IReadOnlyDictionary<bool, Action<Action, Action>> qWorkspaceFrameCloses =
        new Dictionary<bool, Action<Action, Action>>
        {
            [true] = QWorkspacePairClose,
            [false] = (lFlowClose, lViewerClose) => { },
        };

    private static readonly IReadOnlyDictionary<bool, Action<QWorkspaceFrameNotice>> qWorkspaceFrameDetaches =
        new Dictionary<bool, Action<QWorkspaceFrameNotice>>
        {
            [true] = lNotice => lNotice.QWorkspaceFrameDetach(),
            [false] = lNotice => { },
        };

    private readonly bool qWorkspaceFlowPresent;
    private readonly Action qWorkspaceCloser;
    private readonly QWorkspaceFrameNotice qWorkspaceFrameNotice;
    private readonly Action qWorkspaceSurfaceCloser;
    private readonly Action qWorkspaceFlowCloser;
    private readonly Action qWorkspaceViewerCloser;

    // The tab's flow is fixed when it is built, so whether it has one is handed once.
    internal QWorkspaceTeardown(
        bool lFlowPresent,
        Action lWorkspaceClose,
        QWorkspaceFrameNotice lNotice,
        Action lSurfaceClose,
        Action lFlowClose,
        Action lViewerClose)
    {
        qWorkspaceFlowPresent = lFlowPresent;
        qWorkspaceCloser = lWorkspaceClose;
        qWorkspaceFrameNotice = lNotice;
        qWorkspaceSurfaceCloser = lSurfaceClose;
        qWorkspaceFlowCloser = lFlowClose;
        qWorkspaceViewerCloser = lViewerClose;
    }

    public void QWorkspaceFrameClose()
    {
        QWorkspaceStepRun(
            "subscriptions",
            () => qWorkspaceFrameDetaches[qWorkspaceFlowPresent](qWorkspaceFrameNotice));
        QWorkspaceStepRun("deportment", qWorkspaceCloser);
        QWorkspaceStepRun("surface", qWorkspaceSurfaceCloser);
        qWorkspaceFrameCloses[qWorkspaceFlowPresent](qWorkspaceFlowCloser, qWorkspaceViewerCloser);
    }

    private static void QWorkspacePairClose(Action lFlowClose, Action lViewerClose)
    {
        QWorkspaceStepRun("flow", lFlowClose);
        QWorkspaceStepRun("viewer", lViewerClose);
    }

    private static void QWorkspaceStepRun(string lStepName, Action lStep)
    {
        try
        {
            lStep();
        }
        catch (Exception lStepException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Workspace close step '{lStepName}' failed; teardown continues",
                lStepException);
        }
    }
}
