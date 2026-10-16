namespace Cadroue.ShellEngine;

// The viewer trace texts, and the platform reads the host hands in (a file's name, the clock, the line break).
// A word chosen by a value is chosen here; whether and which record is the gate's.
public sealed class LViewerOutlet : LViewerPort
{
    private readonly Func<string, string?> _lViewerOutletName;

    private readonly Func<DateTimeOffset> _lViewerOutletClock;

    private readonly string _lViewerOutletBreak;

    public LViewerOutlet(Func<string, string?> lName, Func<DateTimeOffset> lClock, string lBreak)
    {
        ArgumentNullException.ThrowIfNull(lName);
        ArgumentNullException.ThrowIfNull(lClock);
        ArgumentNullException.ThrowIfNull(lBreak);
        _lViewerOutletName = lName;
        _lViewerOutletClock = lClock;
        _lViewerOutletBreak = lBreak;
    }

    public bool LViewerVerboseRead() => LTraceEngine.LTraceVerboseSeam();

    public void LViewerRefreshRecord(string lReason) =>
        LTraceEngine.LTraceErrorSeam($"mpv rejected paused preview refresh: {lReason}", null);

    public void LViewerRotatedRecord(string lRotate, bool lHorizontal, bool lVertical, bool lReady) =>
        LTraceEngine.LTraceInfoSeam(
            $"Viewer rotate/flip set: rotate {lRotate}, "
            + $"H {lHorizontal}, V {lVertical}, "
            + $"player {(lReady ? "ready" : "none")}, overlay remapped");

    public void LViewerNoticeRecord(Exception lFailure) =>
        LTraceEngine.LTraceErrorSeam("Viewer media notice handler failed", lFailure);

    public void LViewerHostRecord(int lStamp, string lStage, string lDetail) =>
        LTraceEngine.LTraceDetailSeam(
            true, $"Viewer host [{lStamp}] {lStage}", lDetail);

    public void LViewerStageRecord(string lStage, Exception lFailure) =>
        LTraceEngine.LTraceErrorSeam($"Viewer close stage '{lStage}' failed", lFailure);

    public void LViewerClosedRecord(string lPath) =>
        LTraceEngine.LTraceInfoSeam(string.IsNullOrWhiteSpace(lPath)
            ? "Media closed"
            : $"Media closed '{_lViewerOutletName(lPath)}' [{lPath}]");

    public void LViewerDetachedRecord(bool lReady) =>
        LTraceEngine.LTraceInfoSeam($"Viewer host detached: player {(lReady ? "released" : "none")}");

    public void LViewerLoadRecord(int lSerial, string lPath, string lEngine) =>
        LTraceEngine.LTraceDetailSeam(
            false,
            $"Video load start serial={lSerial} '{_lViewerOutletName(lPath)}'",
            $"engine={lEngine}, probing (ffprobe)…");

    public void LViewerProbeRecord(string lKind, string lPath, string? lError) =>
        LTraceEngine.LTraceDetailSeam(false, $"Video probe outcome {lKind} '{_lViewerOutletName(lPath)}'", lError);

    public void LViewerSupersededRecord(int lSerial, bool lUnloaded) =>
        LTraceEngine.LTraceInfoSeam(
            "Video probe outcome discarded (stale/superseded): "
            + $"serial={lSerial}, unloaded={lUnloaded}");

    public void LViewerDiscardedRecord(int lSerial, int lCurrent, bool lUnloaded) =>
        LTraceEngine.LTraceInfoSeam(
            "Player open discarded (stale/superseded): "
            + $"serial got={lSerial} now={lCurrent}, unloaded={lUnloaded}");

    public void LViewerReusedRecord() =>
        LTraceEngine.LTraceInfoSeam("Viewer player reused: same player kept on the host, no swap chain rebuild");

    public void LViewerSwappedRecord(bool lReady) =>
        LTraceEngine.LTraceInfoSeam($"Viewer player swapped: next {(lReady ? "ready" : "none")}");

    public void LViewerPreviewRecord(string lRotate, bool lHorizontal, bool lVertical) =>
        LTraceEngine.LTraceInfoSeam(
            $"Viewer preview restored: rotate {lRotate}, "
            + $"H {lHorizontal}, V {lVertical}");

    public void LViewerRejectedRecord(TimeSpan lPosition, string lReason) =>
        LTraceEngine.LTraceErrorSeam(
            $"Preview seek to {lPosition:hh\\:mm\\:ss\\.fff} was rejected: {lReason}", null);

    public void LViewerSeekRecord(TimeSpan lPosition, bool lDirect, bool lRunning) =>
        LTraceEngine.LTraceDetailSeam(
            true, LViewerSummaryFormat(lPosition), LViewerDetailFormat(lDirect, lRunning));

    // One buffered drag seek, stamped now: its summary line, then its detail line indented under the stamp.
    public string LViewerSeekFormat(TimeSpan lPosition, bool lDirect, bool lRunning)
    {
        string lTime = _lViewerOutletClock().ToString(
            "HH:mm:ss.fff",
            System.Globalization.CultureInfo.InvariantCulture);
        return $"{lTime}  {LViewerSummaryFormat(lPosition)}"
            + _lViewerOutletBreak
            + $"{new string(' ', 14)}{LViewerDetailFormat(lDirect, lRunning)}";
    }

    public void LViewerDragRecord(TimeSpan lFinal, IReadOnlyList<string> lLines) =>
        LTraceEngine.LTraceDetailSeam(
            true,
            lLines.Count == 1
                ? $"Seek accurate to {lFinal:hh\\:mm\\:ss\\.fff}"
                : $"Seek accurate while dragging to {lFinal:hh\\:mm\\:ss\\.fff} ({lLines.Count} requests)",
            string.Join(_lViewerOutletBreak, lLines));

    public void LViewerHiddenRecord(bool lHostVisible, bool lMpvShown, double lWidth, double lHeight) =>
        LTraceEngine.LTraceInfoSeam(
            $"mpv overlay closed (host {(lHostVisible ? "visible" : "hidden")}, "
            + $"shown={lMpvShown}, size {lWidth:0}x{lHeight:0})");

    public void LViewerShownRecord(double lWidth, double lHeight) =>
        LTraceEngine.LTraceDetailSeam(
            false,
            "mpv overlay opened",
            $"top-level transparent window over {lWidth:0}x{lHeight:0}");

    public void LViewerRequestedRecord(string lPath, bool lActive, bool lUnloaded, string lEngine) =>
        LTraceEngine.LTraceDetailSeam(
            false,
            $"Viewer source open requested '{_lViewerOutletName(lPath)}'",
            $"command={(lActive ? "active" : "INACTIVE")}, unloaded={lUnloaded}, "
            + $"engine={lEngine}, path={lPath}");

    public void LViewerRefusedRecord(bool lActive) =>
        LTraceEngine.LTraceWarningSeam("Viewer source open refused: " + (lActive
            ? "empty path"
            : "viewer command inactive (tab not the front workspace)"));

    public void LViewerUnresolvedRecord() =>
        LTraceEngine.LTraceWarningSeam(
            "Viewer source open refused: sidecar (.cad) source could not be resolved");

    private static string LViewerSummaryFormat(TimeSpan lPosition) =>
        $"Seek accurate to {lPosition:hh\\:mm\\:ss\\.fff}";

    private static string LViewerDetailFormat(bool lDirect, bool lRunning) =>
        lDirect
            ? "mpv engine seeks directly"
            : lRunning
                ? "a seek was still running; queued for Flyleaf to conflate"
                : "no seek was in flight";
}
