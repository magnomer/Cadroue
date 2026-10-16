namespace Cadroue.UIDeportment;

public sealed class QViewerTeardown
{
    private readonly QViewer qViewer;
    private readonly QViewerMedia qViewerMedia;
    private readonly Action qViewerProbe;
    private readonly Action<string, Exception> qViewerTrace;

    public QViewerTeardown(
        QViewer lOwner,
        QViewerMedia lMedia,
        Action lProbeDispose,
        Action<string, Exception> lErrorRecord)
    {
        qViewer = lOwner;
        qViewerMedia = lMedia;
        qViewerProbe = lProbeDispose;
        qViewerTrace = lErrorRecord;
    }

    public void QViewerClose()
    {
        if (qViewer.QViewerUnloaded)
        {
            return;
        }

        qViewer.QViewerUnloadSet();
        qViewer.QViewerSerialChange();
        QViewerStageRun("media probe", qViewerProbe);
        QViewerStageRun("player", qViewerMedia.QViewerPlayerStop);
    }

    public void QViewerStageRun(string lStage, Action lAction)
    {
        try
        {
            lAction();
        }
        catch (Exception lException)
        {
            qViewerTrace(lStage, lException);
        }
    }
}
