namespace Cadroue.UIDeportment;

public sealed class QViewerTeardown
{
    private readonly LViewer lViewer;
    private readonly LViewerMedia lViewerMedia;
    private readonly Action lViewerProbe;
    private readonly Action<string, Exception?> lViewerTrace;

    public QViewerTeardown(
        LViewer lOwner,
        LViewerMedia lMedia,
        Action lProbeDispose,
        Action<string, Exception?> lErrorRecord)
    {
        lViewer = lOwner;
        lViewerMedia = lMedia;
        lViewerProbe = lProbeDispose;
        lViewerTrace = lErrorRecord;
    }

    public void LViewerClose()
    {
        if (lViewer.LViewerUnloaded)
        {
            return;
        }

        lViewer.LViewerUnloadSet();
        lViewer.LViewerSerialChange();
        LViewerStageRun("media probe", lViewerProbe);
        LViewerStageRun("player", lViewerMedia.LViewerPlayerStop);
    }

    public void LViewerStageRun(string lStage, Action lAction)
    {
        try
        {
            lAction();
        }
        catch (Exception lException)
        {
            lViewerTrace($"Viewer close stage '{lStage}' failed", lException);
        }
    }
}
