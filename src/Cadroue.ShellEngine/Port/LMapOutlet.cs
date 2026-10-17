using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the map gate: every member forwards to the same-named member of the engine's map facade.
// It rejects a null engine and keeps it for every forward.
public sealed class LMapOutlet : LMapPort
{
    private readonly LEngine _lMapOutletEngine;

    public LMapOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lMapOutletEngine = lEngine;
    }

    public (TimeSpan LMapOrigin, TimeSpan LMapLimit, TimeSpan LMapDuration) LMapRangeRead(LSpool lRange) =>
        _lMapOutletEngine.LEngineMap.LMapRangeRead(lRange);

    public double LMapRatioResolve(LSpool lRange, TimeSpan lTime) =>
        _lMapOutletEngine.LEngineMap.LMapRatioResolve(lRange, lTime);

    public TimeSpan LMapTimeResolve(LSpool lRange, double lRatio) =>
        _lMapOutletEngine.LEngineMap.LMapTimeResolve(lRange, lRatio);

    public void LMapOriginSet(LSpool lRange, TimeSpan lOrigin) =>
        _lMapOutletEngine.LEngineMap.LMapOriginSet(lRange, lOrigin);

    public void LMapLimitSet(LSpool lRange, TimeSpan lLimit) =>
        _lMapOutletEngine.LEngineMap.LMapLimitSet(lRange, lLimit);

    public void LMapBodyMove(LSpool lRange, TimeSpan lStep) => _lMapOutletEngine.LEngineMap.LMapBodyMove(lRange, lStep);

    public IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapEnd, int LMapColor, bool LMapHidden)> LMapSectionsRead(
        LSegment lSegment) => _lMapOutletEngine.LEngineMap.LMapSectionsRead(lSegment);

    public IReadOnlyList<(TimeSpan LMapOrigin, TimeSpan LMapLimit)> LMapCoverageRead(
        IReadOnlyList<LKeyframeScanRange> lRanges, LSpool lRange) =>
        _lMapOutletEngine.LEngineMap.LMapCoverageRead(lRanges, lRange);

    public void LMapDrawRecord(TimeSpan lCursor, string? lSource, string lTrigger, double lMilliseconds, int lGlyphs) =>
        _lMapOutletEngine.LEngineMap.LMapDrawRecord(lCursor, lSource, lTrigger, lMilliseconds, lGlyphs);
}
