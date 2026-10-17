namespace Cadroue.ShellEngine;

// The engine's face for the viewer gate: every member forwards to the same-named member of the engine's viewer
// facade. It rejects a null engine and keeps it for every forward.
public sealed class LViewerOutlet : LViewerPort
{
    private readonly LEngine _lViewerOutletEngine;

    public LViewerOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lViewerOutletEngine = lEngine;
    }

    public bool LViewerVerboseRead() => _lViewerOutletEngine.LEngineViewer.LViewerVerboseRead();

    public void LViewerRefreshRecord(string lReason) =>
        _lViewerOutletEngine.LEngineViewer.LViewerRefreshRecord(lReason);

    public void LViewerRotatedRecord(string lRotate, bool lHorizontal, bool lVertical, bool lReady) =>
        _lViewerOutletEngine.LEngineViewer.LViewerRotatedRecord(lRotate, lHorizontal, lVertical, lReady);

    public void LViewerNoticeRecord(Exception lFailure) =>
        _lViewerOutletEngine.LEngineViewer.LViewerNoticeRecord(lFailure);

    public void LViewerHostRecord(int lStamp, string lStage, string lDetail) =>
        _lViewerOutletEngine.LEngineViewer.LViewerHostRecord(lStamp, lStage, lDetail);

    public void LViewerStageRecord(string lStage, Exception lFailure) =>
        _lViewerOutletEngine.LEngineViewer.LViewerStageRecord(lStage, lFailure);

    public void LViewerClosedRecord(string lPath) =>
        _lViewerOutletEngine.LEngineViewer.LViewerClosedRecord(lPath);

    public void LViewerDetachedRecord(bool lReady) =>
        _lViewerOutletEngine.LEngineViewer.LViewerDetachedRecord(lReady);

    public void LViewerLoadRecord(int lSerial, string lPath, string lEngine) =>
        _lViewerOutletEngine.LEngineViewer.LViewerLoadRecord(lSerial, lPath, lEngine);

    public void LViewerProbeRecord(string lKind, string lPath, string? lError) =>
        _lViewerOutletEngine.LEngineViewer.LViewerProbeRecord(lKind, lPath, lError);

    public void LViewerSupersededRecord(int lSerial, bool lUnloaded) =>
        _lViewerOutletEngine.LEngineViewer.LViewerSupersededRecord(lSerial, lUnloaded);

    public void LViewerDiscardedRecord(int lSerial, int lCurrent, bool lUnloaded) =>
        _lViewerOutletEngine.LEngineViewer.LViewerDiscardedRecord(lSerial, lCurrent, lUnloaded);

    public void LViewerReusedRecord() => _lViewerOutletEngine.LEngineViewer.LViewerReusedRecord();

    public void LViewerSwappedRecord(bool lReady) =>
        _lViewerOutletEngine.LEngineViewer.LViewerSwappedRecord(lReady);

    public void LViewerPreviewRecord(string lRotate, bool lHorizontal, bool lVertical) =>
        _lViewerOutletEngine.LEngineViewer.LViewerPreviewRecord(lRotate, lHorizontal, lVertical);

    public void LViewerRejectedRecord(TimeSpan lPosition, string lReason) =>
        _lViewerOutletEngine.LEngineViewer.LViewerRejectedRecord(lPosition, lReason);

    public void LViewerSeekRecord(TimeSpan lPosition, bool lDirect, bool lRunning) =>
        _lViewerOutletEngine.LEngineViewer.LViewerSeekRecord(lPosition, lDirect, lRunning);

    public string LViewerSeekFormat(TimeSpan lPosition, bool lDirect, bool lRunning) =>
        _lViewerOutletEngine.LEngineViewer.LViewerSeekFormat(lPosition, lDirect, lRunning);

    public void LViewerDragRecord(TimeSpan lFinal, IReadOnlyList<string> lLines) =>
        _lViewerOutletEngine.LEngineViewer.LViewerDragRecord(lFinal, lLines);

    public void LViewerHiddenRecord(bool lHostVisible, bool lMpvShown, double lWidth, double lHeight) =>
        _lViewerOutletEngine.LEngineViewer.LViewerHiddenRecord(lHostVisible, lMpvShown, lWidth, lHeight);

    public void LViewerShownRecord(double lWidth, double lHeight) =>
        _lViewerOutletEngine.LEngineViewer.LViewerShownRecord(lWidth, lHeight);

    public void LViewerRequestedRecord(string lPath, bool lActive, bool lUnloaded, string lEngine) =>
        _lViewerOutletEngine.LEngineViewer.LViewerRequestedRecord(lPath, lActive, lUnloaded, lEngine);

    public void LViewerRefusedRecord(bool lActive) =>
        _lViewerOutletEngine.LEngineViewer.LViewerRefusedRecord(lActive);

    public void LViewerUnresolvedRecord() => _lViewerOutletEngine.LEngineViewer.LViewerUnresolvedRecord();
}
