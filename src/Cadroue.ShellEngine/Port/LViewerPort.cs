namespace Cadroue.ShellEngine;

public interface LViewerPort
{
    bool LViewerVerboseRead();

    void LViewerRefreshRecord(string lReason);

    void LViewerRotatedRecord(string lRotate, bool lHorizontal, bool lVertical, bool lReady);

    void LViewerNoticeRecord(Exception lFailure);

    void LViewerHostRecord(int lStamp, string lStage, string lDetail);

    void LViewerStageRecord(string lStage, Exception lFailure);

    void LViewerClosedRecord(string lPath);

    void LViewerDetachedRecord(bool lReady);

    void LViewerLoadRecord(int lSerial, string lPath, string lEngine);

    void LViewerProbeRecord(string lKind, string lPath, string? lError);

    void LViewerSupersededRecord(int lSerial, bool lUnloaded);

    void LViewerDiscardedRecord(int lSerial, int lCurrent, bool lUnloaded);

    void LViewerReusedRecord();

    void LViewerSwappedRecord(bool lReady);

    void LViewerPreviewRecord(string lRotate, bool lHorizontal, bool lVertical);

    void LViewerRejectedRecord(TimeSpan lPosition, string lReason);

    void LViewerSeekRecord(TimeSpan lPosition, bool lDirect, bool lRunning);

    string LViewerSeekFormat(TimeSpan lPosition, bool lDirect, bool lRunning);

    void LViewerDragRecord(TimeSpan lFinal, IReadOnlyList<string> lLines);

    void LViewerHiddenRecord(bool lHostVisible, bool lMpvShown, double lWidth, double lHeight);

    void LViewerShownRecord(double lWidth, double lHeight);

    void LViewerRequestedRecord(string lPath, bool lActive, bool lUnloaded, string lEngine);

    void LViewerRefusedRecord(bool lActive);

    void LViewerUnresolvedRecord();
}
