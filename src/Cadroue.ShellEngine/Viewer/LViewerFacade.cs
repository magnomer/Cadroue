using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The viewer trace texts, recorded through the rig's trace; a file's name, the clock and the line break are read
// here. A word chosen by a value is chosen here; whether and which record is the gate's.
public sealed class LViewerFacade
{
    private readonly LRigTrace _lViewerFacadeTrace;

    internal LViewerFacade(LRigTrace lTrace)
    {
        ArgumentNullException.ThrowIfNull(lTrace);
        _lViewerFacadeTrace = lTrace;
    }

    public bool LViewerVerboseRead() => _lViewerFacadeTrace.LRigTraceVerbose();

    public void LViewerRefreshRecord(string lReason) =>
        _lViewerFacadeTrace.LRigTraceError($"mpv rejected paused preview refresh: {lReason}", null);

    public void LViewerRotatedRecord(string lRotate, bool lHorizontal, bool lVertical, bool lReady) =>
        _lViewerFacadeTrace.LRigTraceInfo(
            $"Viewer rotate/flip set: rotate {lRotate}, "
            + $"H {lHorizontal}, V {lVertical}, "
            + $"player {(lReady ? "ready" : "none")}, overlay remapped");

    public void LViewerNoticeRecord(Exception lFailure) =>
        _lViewerFacadeTrace.LRigTraceError("Viewer media notice handler failed", lFailure);

    public void LViewerHostRecord(int lStamp, string lStage, string lDetail) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            true, $"Viewer host [{lStamp}] {lStage}", lDetail);

    public void LViewerStageRecord(string lStage, Exception lFailure) =>
        _lViewerFacadeTrace.LRigTraceError($"Viewer close stage '{lStage}' failed", lFailure);

    public void LViewerClosedRecord(string lPath) =>
        _lViewerFacadeTrace.LRigTraceInfo(string.IsNullOrWhiteSpace(lPath)
            ? "Media closed"
            : $"Media closed '{Path.GetFileName(lPath)}' [{lPath}]");

    public void LViewerDetachedRecord(bool lReady) =>
        _lViewerFacadeTrace.LRigTraceInfo($"Viewer host detached: player {(lReady ? "released" : "none")}");

    public void LViewerLoadRecord(int lSerial, string lPath, string lEngine) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            false,
            $"Video load start serial={lSerial} '{Path.GetFileName(lPath)}'",
            $"engine={lEngine}, probing (ffprobe)…");

    public void LViewerProbeRecord(string lKind, string lPath, string? lError) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            false, $"Video probe outcome {lKind} '{Path.GetFileName(lPath)}'", lError);

    public void LViewerSupersededRecord(int lSerial, bool lUnloaded) =>
        _lViewerFacadeTrace.LRigTraceInfo(
            "Video probe outcome discarded (stale/superseded): "
            + $"serial={lSerial}, unloaded={lUnloaded}");

    public void LViewerDiscardedRecord(int lSerial, int lCurrent, bool lUnloaded) =>
        _lViewerFacadeTrace.LRigTraceInfo(
            "Player open discarded (stale/superseded): "
            + $"serial got={lSerial} now={lCurrent}, unloaded={lUnloaded}");

    public void LViewerReusedRecord() =>
        _lViewerFacadeTrace.LRigTraceInfo("Viewer player reused: same player kept on the host, no swap chain rebuild");

    public void LViewerSwappedRecord(bool lReady) =>
        _lViewerFacadeTrace.LRigTraceInfo($"Viewer player swapped: next {(lReady ? "ready" : "none")}");

    public void LViewerPreviewRecord(string lRotate, bool lHorizontal, bool lVertical) =>
        _lViewerFacadeTrace.LRigTraceInfo(
            $"Viewer preview restored: rotate {lRotate}, "
            + $"H {lHorizontal}, V {lVertical}");

    public void LViewerRejectedRecord(TimeSpan lPosition, string lReason) =>
        _lViewerFacadeTrace.LRigTraceError(
            $"Preview seek to {lPosition:hh\\:mm\\:ss\\.fff} was rejected: {lReason}", null);

    public void LViewerSeekRecord(TimeSpan lPosition, bool lDirect, bool lRunning) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            true, LViewerSummaryFormat(lPosition), LViewerDetailFormat(lDirect, lRunning));

    // One buffered drag seek, stamped now: its summary line, then its detail line indented under the stamp.
    public string LViewerSeekFormat(TimeSpan lPosition, bool lDirect, bool lRunning)
    {
        string lTime = DateTimeOffset.Now.ToString(
            "HH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture);
        return $"{lTime}  {LViewerSummaryFormat(lPosition)}"
            + Environment.NewLine
            + $"{new string(' ', 14)}{LViewerDetailFormat(lDirect, lRunning)}";
    }

    public void LViewerDragRecord(TimeSpan lFinal, IReadOnlyList<string> lLines) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            true,
            lLines.Count == 1
                ? $"Seek accurate to {lFinal:hh\\:mm\\:ss\\.fff}"
                : $"Seek accurate while dragging to {lFinal:hh\\:mm\\:ss\\.fff} ({lLines.Count} requests)",
            string.Join(Environment.NewLine, lLines));

    public void LViewerHiddenRecord(bool lHostVisible, bool lMpvShown, double lWidth, double lHeight) =>
        _lViewerFacadeTrace.LRigTraceInfo(
            $"mpv overlay closed (host {(lHostVisible ? "visible" : "hidden")}, "
            + $"shown={lMpvShown}, size {lWidth:0}x{lHeight:0})");

    public void LViewerShownRecord(double lWidth, double lHeight) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            false,
            "mpv overlay opened",
            $"top-level transparent window over {lWidth:0}x{lHeight:0}");

    public void LViewerRequestedRecord(string lPath, bool lActive, bool lUnloaded, string lEngine) =>
        _lViewerFacadeTrace.LRigTraceDetail(
            false,
            $"Viewer source open requested '{Path.GetFileName(lPath)}'",
            $"command={(lActive ? "active" : "INACTIVE")}, unloaded={lUnloaded}, "
            + $"engine={lEngine}, path={lPath}");

    public void LViewerRefusedRecord(bool lActive) =>
        _lViewerFacadeTrace.LRigTraceWarning("Viewer source open refused: " + (lActive
            ? "empty path"
            : "viewer command inactive (tab not the front workspace)"), null);

    public void LViewerUnresolvedRecord() =>
        _lViewerFacadeTrace.LRigTraceWarning(
            "Viewer source open refused: sidecar (.cad) source could not be resolved", null);

    private static string LViewerSummaryFormat(TimeSpan lPosition) =>
        $"Seek accurate to {lPosition:hh\\:mm\\:ss\\.fff}";

    private static string LViewerDetailFormat(bool lDirect, bool lRunning) =>
        lDirect
            ? "mpv engine seeks directly"
            : lRunning
                ? "a seek was still running; queued for Flyleaf to conflate"
                : "no seek was in flight";
}
