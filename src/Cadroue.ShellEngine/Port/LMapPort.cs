using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The map gate's reach into the range it draws and edits: the range, the sections and the scans pass through unread
// and come back as plain values, and the edits run on the range itself.
public interface LMapPort
{
    // The range's origin and limit and the length of the media under it.
    (TimeSpan LMapOrigin, TimeSpan LMapLimit, TimeSpan LMapDuration) LMapRangeRead(LSpool lRange);

    // Where a time sits along the media, as a share of its length.
    double LMapRatioResolve(LSpool lRange, TimeSpan lTime);

    // The time a share of the media's length stands for.
    TimeSpan LMapTimeResolve(LSpool lRange, double lRatio);

    void LMapOriginSet(LSpool lRange, TimeSpan lOrigin);

    void LMapLimitSet(LSpool lRange, TimeSpan lLimit);

    // The range moves whole by this step.
    void LMapBodyMove(LSpool lRange, TimeSpan lStep);

    // Each section's span with its colour and whether it is hidden, in store order.
    IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapEnd, int LMapColor, bool LMapHidden)> LMapSectionsRead(
        LSegment lSegment);

    // The spans the keyframe scans covered over the whole media.
    IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapLimit)> LMapCoverageRead(
        IReadOnlyList<LKeyframeScanRange> lRanges, LSpool lRange);

    // One drawn map on the trace timeline: the cursor, the source, what asked for the draw, its time and its bands.
    void LMapDrawRecord(TimeSpan lCursor, string? lSource, string lTrigger, double lMilliseconds, int lGlyphs);
}
