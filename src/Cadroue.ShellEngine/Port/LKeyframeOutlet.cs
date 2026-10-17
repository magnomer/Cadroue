using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// One flow's keyframe scan: the calls over one scanner the rig hands in, the keyframes and ranges of the last
// current notice, and each scan's trace. The source the traces name comes from the flow gate; whether a scan or a
// move runs is the gate's.
public sealed class LKeyframeOutlet : LKeyframePort
{
    private readonly Func<string> _lKeyframeOutletSource;
    private readonly LRigKeyframe _lKeyframeOutletRig;
    private string? _lKeyframeOutletStamp;

    public LKeyframeOutlet(Func<string> lSource, LRigKeyframe lRig)
    {
        ArgumentNullException.ThrowIfNull(lSource);
        ArgumentNullException.ThrowIfNull(lRig);
        _lKeyframeOutletSource = lSource;
        _lKeyframeOutletRig = lRig;
        lRig.LRigKeyframeAttachment(LKeyframeHandle);
    }

    public event Action<LKeyframeNotice>? LKeyframeArrived;

    public IReadOnlyList<LKeyframeEntry> LKeyframeEntries { get; private set; } = [];

    public IReadOnlyList<LKeyframeScanRange> LKeyframeRanges { get; private set; } = [];

    public TimeSpan LKeyframeDelayRead() =>
        TimeSpan.FromMilliseconds(LPreference.LPreferenceStateCurrent.LPreferenceKeyframeDelay);

    public void LKeyframeStart(string lSource, LMediaInfo lMedia, TimeSpan lCursor, TimeSpan lDuration)
    {
        _lKeyframeOutletRig.LRigKeyframeWork(
            $"Keyframe scan requested around {lCursor:hh\\:mm\\:ss\\.fff}",
            $"source {_lKeyframeOutletSource()}, duration {lDuration:hh\\:mm\\:ss}\n"
            + $"window {LKeyframeView.LKeyframeRangeBefore:hh\\:mm\\:ss} before to "
            + $"{LKeyframeView.LKeyframeRangeAfter:hh\\:mm\\:ss} after the cursor");
        _lKeyframeOutletRig.LRigKeyframeScanner(lSource, lMedia, lCursor);
    }

    public void LKeyframeSuspend() => _lKeyframeOutletRig.LRigKeyframeSuspension();

    public void LKeyframeSync(TimeSpan lCursor) => _lKeyframeOutletRig.LRigKeyframeCursor(lCursor);

    public bool LKeyframeApply(LKeyframeNotice lNotice)
    {
        if (lNotice.LKeyframeSerial != _lKeyframeOutletRig.LRigKeyframeSerial())
        {
            return false;
        }

        LKeyframeRecord(lNotice);
        LKeyframeEntries = lNotice.LKeyframeList;
        LKeyframeRanges = lNotice.LKeyframeRanges;
        return true;
    }

    public void LKeyframeClear()
    {
        LKeyframeEntries = [];
        LKeyframeRanges = [];
    }

    public (bool LKeyframeFailed, bool LKeyframeReady, TimeSpan? LKeyframeTarget) LKeyframeMove(
        int lDirection, TimeSpan lCursor)
    {
        (bool lReady, TimeSpan? lTarget, bool lFailed) = lDirection switch
        {
            < 0 => _lKeyframeOutletRig.LRigKeyframePrevious(lCursor),
            > 0 => _lKeyframeOutletRig.LRigKeyframeNext(lCursor),
            _ => _lKeyframeOutletRig.LRigKeyframeNearest(lCursor)
        };
        if (lFailed)
        {
            _lKeyframeOutletRig.LRigKeyframeWarning(
                "Keyframe navigation unavailable: the scan around the cursor failed repeatedly",
                $"source {_lKeyframeOutletSource()}, cursor {lCursor:hh\\:mm\\:ss\\.fff}");
        }

        return (lFailed, lReady, lTarget);
    }

    public void LKeyframeClose() => _lKeyframeOutletRig.LRigKeyframeDisposal(LKeyframeHandle);

    private void LKeyframeHandle(LKeyframeNotice lNotice) => LKeyframeArrived?.Invoke(lNotice);

    // A notice equal to the last one traced is kept without a second trace.
    private void LKeyframeRecord(LKeyframeNotice lNotice)
    {
        double lScanned = lNotice.LKeyframeRanges.Sum(
            lRange => (lRange.LKeyframeRangeLimit - lRange.LKeyframeRangeOrigin).TotalSeconds);
        string lStamp =
            $"{lNotice.LKeyframeKind}/{lNotice.LKeyframeList.Count}/{lNotice.LKeyframeRanges.Count}/{lScanned:0.###}";
        if (string.Equals(_lKeyframeOutletStamp, lStamp, StringComparison.Ordinal))
        {
            return;
        }

        _lKeyframeOutletStamp = lStamp;
        string lSource = _lKeyframeOutletSource();
        LTraceEngine.LTraceInfoSeam(lNotice.LKeyframeKind switch
        {
            LKeyframeKind.LKeyframeKindIntra =>
                $"Keyframe scan '{lSource}': every frame is a keyframe, no scan needed",
            LKeyframeKind.LKeyframeKindNone =>
                $"Keyframe scan '{lSource}': no video stream, no scan needed",
            _ => $"Keyframe scan '{lSource}': {lNotice.LKeyframeList.Count} keyframe(s) known, "
                + $"{TimeSpan.FromSeconds(lScanned):hh\\:mm\\:ss} scanned across "
                + $"{lNotice.LKeyframeRanges.Count} range(s)"
        });
    }
}
