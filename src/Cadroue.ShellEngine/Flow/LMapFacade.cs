using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The map gate's engine side over the engine range: reads and edits go to the range, the scans to the keyframe view,
// and each drawn map to the rig's trace timeline.
public sealed class LMapFacade : LMapPort
{
    private readonly LRigTrace _lMapFacadeTrace;

    internal LMapFacade(LRigTrace lTrace)
    {
        ArgumentNullException.ThrowIfNull(lTrace);
        _lMapFacadeTrace = lTrace;
    }

    public (TimeSpan LMapOrigin, TimeSpan LMapLimit, TimeSpan LMapDuration) LMapRangeRead(LSpool lRange) =>
        (lRange.LSpoolRangeOrigin, lRange.LSpoolRangeLimit, lRange.LSpoolDuration);

    public double LMapRatioResolve(LSpool lRange, TimeSpan lTime) => lRange.LSpoolRatioResolve(lTime);

    public TimeSpan LMapTimeResolve(LSpool lRange, double lRatio) => lRange.LSpoolTimeResolve(lRatio);

    public void LMapOriginSet(LSpool lRange, TimeSpan lOrigin) => lRange.LSpoolStartSet(lOrigin);

    public void LMapLimitSet(LSpool lRange, TimeSpan lLimit) => lRange.LSpoolEndSet(lLimit);

    public void LMapBodyMove(LSpool lRange, TimeSpan lStep) => lRange.LSpoolMove(lStep);

    public IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapEnd, int LMapColor, bool LMapHidden)> LMapSectionsRead(
        LSegment lSegment) =>
        lSegment.LSegmentListRead()
            .Select(lPiece => (lPiece.LPieceOrigin, lPiece.LPieceEnd, lPiece.LPieceColorIndex, lPiece.LPieceHidden))
            .ToArray();

    public IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapLimit)> LMapCoverageRead(
        IReadOnlyList<LKeyframeScanRange> lRanges, LSpool lRange) =>
        LKeyframeView.LKeyframeCoverageResolve(lRanges, lRange, true)
            .Select(lScan => (lScan.LKeyframeRangeOrigin, lScan.LKeyframeRangeLimit))
            .ToArray();

    public void LMapDrawRecord(TimeSpan lCursor, string? lSource, string lTrigger, double lMilliseconds, int lGlyphs) =>
        _lMapFacadeTrace.LRigTraceTimeline("Map", lCursor, lSource, lTrigger, lMilliseconds, lGlyphs);
}
