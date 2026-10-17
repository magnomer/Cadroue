using System;
using System.Collections.Generic;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// What a point over the map grabs: the range's origin or limit handle, its body, or the cursor anywhere else.
public enum CMapHit
{
    CMapHitNone,
    CMapHitOrigin,
    CMapHitLimit,
    CMapHitBody,
    CMapHitCursor,
}

// One section's band on the map rail, in pixels, with its colour, its number and whether it shows and is selected.
public sealed record CMapBand(
    double CMapBandLeft,
    double CMapBandTop,
    double CMapBandRight,
    double CMapBandBottom,
    int CMapBandColor,
    string CMapBandBadge,
    bool CMapBandShown,
    bool CMapBandSelected);

// The map's answer for one size, in pixels. Sized: the background shows. Railed: a range is held and the rail fits,
// so the rail, its outline, the bands, the coverage strip (top and bottom) and its scans (left and width) show.
// Body: the navigator over the range, as
// left, width and rail height, with the cursor beside it; null while the range has no width.
public sealed record CMapFrame(
    bool CMapFrameSized,
    bool CMapFrameRailed,
    double CMapFrameBottom,
    (double, double) CMapFrameCoverage,
    bool CMapFrameWaveform,
    (double, double, IReadOnlyList<(double, double)>) CMapFrameOutline,
    IReadOnlyList<CMapBand> CMapFrameBands,
    IReadOnlyList<(double, double)> CMapFrameScans,
    (double, double, double)? CMapFrameBody,
    IReadOnlyList<double> CMapFrameCursor);

// The map's single home: what a press grabs, the drag it runs on the range, what asked for the last draw and how many
// bands it drew. The driver hands it raw pointer input and the surface's size and paints its answers.
public sealed class CMap
{
    public const double CMapRailTop = 3;
    public const double CMapHandleWidth = 12;
    public const double CMapCoverageHeight = 3;
    private const double CMapRenderLeast = 12;
    private const double CMapSectionInset = 1;

    private static readonly (double, double, IReadOnlyList<(double, double)>) _cMapOutlineEmpty =
        (0, 0, Array.Empty<(double, double)>());

    private readonly LMapPort _cMapPort;
    private readonly CFlow _cMapFlow;
    private CMapHit _cMapDragKind;
    private CMapHit _cMapHoverKind;
    private double _cMapDragOrigin;
    private double _cMapDragPrevious;
    private TimeSpan _cMapDragTime;
    private string _cMapTrigger = "attach";
    private int _cMapGlyphCount;

    internal CMap(LMapPort port, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(flow);
        _cMapPort = port;
        _cMapFlow = flow;
    }

    // Told at a drag's start and at its end, for the driver to hand on with the viewer's playing seam.
    public event Action<bool>? CMapDragChanged;

    // A drag edited the range.
    public event Action? CMapSpoolChanged;

    public CMapHit CMapDragKind => _cMapDragKind;

    public string CMapTrigger => _cMapTrigger;

    public int CMapGlyphCount => _cMapGlyphCount;

    // The grab zone in the middle of the navigator body, as left, width and height.
    public static (double, double, double) CMapMoveResolve(
        double bodyLeft, double bodyWidth, double bodyHeight, double sideWidth, double width)
    {
        double moveHeight = Math.Clamp(bodyHeight, 1, 16);
        double spaceWidth = Math.Max(0, bodyWidth - sideWidth * 2);
        if (spaceWidth <= 0)
        {
            return (0, 0, 0);
        }

        double moveWidth = Math.Clamp(bodyWidth * 0.42, Math.Min(72, spaceWidth), Math.Min(320, spaceWidth));
        double centered = bodyLeft + (bodyWidth - moveWidth) / 2;
        double leftMinimum = bodyLeft + sideWidth;
        double leftMaximum = bodyLeft + bodyWidth - sideWidth - moveWidth;
        double moveLeft = leftMaximum < leftMinimum ? centered : Math.Clamp(centered, leftMinimum, leftMaximum);
        return (Math.Clamp(moveLeft, 0, Math.Max(0, width - moveWidth)), moveWidth, moveHeight);
    }

    public void CMapTriggerSet(string trigger) => _cMapTrigger = trigger;

    // One draw: the frame for this size goes to the paint, then the draw and its time go on the trace timeline.
    public void CMapDraw(double width, double height, Action<CMapFrame> paint, Func<double> elapsed)
    {
        ArgumentNullException.ThrowIfNull(paint);
        ArgumentNullException.ThrowIfNull(elapsed);
        paint(CMapFrameResolve(width, height));
        _cMapPort.LMapDrawRecord(
            _cMapFlow.CFlowCursor, _cMapFlow.CFlowSource, _cMapTrigger, elapsed(), _cMapGlyphCount);
    }

    public CMapHit CMapHoverResolve(double x, double y, double width, double height)
    {
        _cMapHoverKind = CMapHitResolve(x, y, width, height);
        return _cMapHoverKind;
    }

    // Leaving keeps what a drag still grabs; with no drag nothing is grabbed.
    public CMapHit CMapLeaveResolve() => _cMapDragKind == CMapHit.CMapHitNone ? CMapHit.CMapHitNone : _cMapHoverKind;

    public bool CMapPressHandle(double x, double y, double width, double height)
    {
        if (_cMapFlow.CFlowRange is not { } range || width <= 0)
        {
            return false;
        }

        CMapDragStart(x, y, width, height);
        CMapPressDispatch(range, x, width);
        return true;
    }

    public bool CMapMoveHandle(double x, double width)
    {
        if (_cMapFlow.CFlowRange is not { } range || !CMapDragCheck() || width <= 0)
        {
            return false;
        }

        CMapMoveDispatch(range, x, width);
        return true;
    }

    public void CMapDragClear()
    {
        bool dragging = CMapDragCheck();
        _cMapDragKind = CMapHit.CMapHitNone;
        if (dragging)
        {
            CMapDragChanged?.Invoke(false);
        }
    }

    private CMapFrame CMapFrameResolve(double width, double height)
    {
        _cMapGlyphCount = 0;
        var frame = new CMapFrame(
            width > 0 && height > 0, false, 0, (0, 0), false, _cMapOutlineEmpty, [], [], null, []);
        if (!frame.CMapFrameSized
            || _cMapFlow.CFlowRange is not { } range
            || _cMapPort.LMapRangeRead(range).LMapDuration <= TimeSpan.Zero
            || height < CMapRenderLeast)
        {
            return frame;
        }

        double coverageBottom = Math.Max(0, height - 1);
        double coverageTop = Math.Max(0, coverageBottom - CMapCoverageHeight);
        double railBottom = Math.Max(CMapRailTop, coverageTop - 2);
        double railHeight = Math.Max(0, railBottom - CMapRailTop);
        if (railHeight <= 0)
        {
            return frame;
        }

        CMapFrame railed = CMapBaseResolve(frame, range, width, railBottom, railHeight) with
        {
            CMapFrameCoverage = (coverageTop, coverageTop + CMapCoverageHeight),
        };
        _cMapGlyphCount = railed.CMapFrameBands.Count;
        return CMapOverResolve(railed, range, width, coverageTop, railHeight);
    }

    private CMapFrame CMapBaseResolve(CMapFrame frame, LSpool range, double width, double railBottom, double railHeight)
    {
        TimeSpan duration = _cMapPort.LMapRangeRead(range).LMapDuration;
        return frame with
        {
            CMapFrameRailed = true,
            CMapFrameBottom = railBottom,
            CMapFrameWaveform = _cMapFlow.CFlowWaveform.CWaveformShown,
            CMapFrameOutline = _cMapFlow.CFlowWaveform.CWaveformOutlineResolve(
                width, CMapRailTop, railHeight, TimeSpan.Zero, duration),
            CMapFrameBands = CMapBandsResolve(duration, width, railHeight),
        };
    }

    private CMapFrame CMapOverResolve(
        CMapFrame frame, LSpool range, double width, double coverageTop, double railHeight)
    {
        (TimeSpan origin, TimeSpan limit, TimeSpan duration) = _cMapPort.LMapRangeRead(range);
        IReadOnlyList<(double, double)> scans = CMapScansResolve(range, duration, width);
        double bodyLeft = CMapSpoolResolve(range, origin, width);
        double bodyRight = CMapSpoolResolve(range, limit, width);
        double bodyWidth = Math.Max(0, Math.Max(bodyLeft, bodyRight) - Math.Min(bodyLeft, bodyRight));
        if (bodyWidth <= 0)
        {
            return frame with { CMapFrameScans = scans };
        }

        return frame with
        {
            CMapFrameScans = scans,
            CMapFrameBody = (Math.Min(bodyLeft, bodyRight), bodyWidth, railHeight),
            CMapFrameCursor = [CMapSpoolResolve(range, _cMapFlow.CFlowCursor, width)],
        };
    }

    private void CMapDragStart(double x, double y, double width, double height)
    {
        _cMapDragOrigin = x;
        _cMapDragPrevious = x;
        _cMapDragKind = CMapHitResolve(x, y, width, height);
        _cMapHoverKind = _cMapDragKind;
    }

    // A handle remembers where its edge stood; the drag is told before a cursor grab seeks.
    private void CMapPressDispatch(LSpool range, double x, double width)
    {
        switch (_cMapDragKind)
        {
            case CMapHit.CMapHitOrigin:
                _cMapDragTime = _cMapPort.LMapRangeRead(range).LMapOrigin;
                break;
            case CMapHit.CMapHitLimit:
                _cMapDragTime = _cMapPort.LMapRangeRead(range).LMapLimit;
                break;
        }

        CMapDragChanged?.Invoke(true);
        if (_cMapDragKind == CMapHit.CMapHitCursor)
        {
            _cMapFlow.CFlowCursorSeek(CMapTimeResolve(range, x, width));
        }
    }

    private void CMapMoveDispatch(LSpool range, double x, double width)
    {
        TimeSpan dragDelta = _cMapPort.LMapTimeResolve(range, (x - _cMapDragOrigin) / width);
        switch (_cMapDragKind)
        {
            case CMapHit.CMapHitOrigin:
                _cMapPort.LMapOriginSet(range, _cMapDragTime + dragDelta);
                CMapSpoolChanged?.Invoke();
                break;
            case CMapHit.CMapHitLimit:
                _cMapPort.LMapLimitSet(range, _cMapDragTime + dragDelta);
                CMapSpoolChanged?.Invoke();
                break;
            case CMapHit.CMapHitBody:
                _cMapPort.LMapBodyMove(range, _cMapPort.LMapTimeResolve(range, (x - _cMapDragPrevious) / width));
                _cMapDragPrevious = x;
                CMapSpoolChanged?.Invoke();
                break;
            case CMapHit.CMapHitCursor:
                _cMapFlow.CFlowCursorSeek(CMapTimeResolve(range, x, width));
                break;
        }
    }

    private bool CMapDragCheck() => _cMapDragKind != CMapHit.CMapHitNone;

    private TimeSpan CMapTimeResolve(LSpool range, double x, double width) =>
        _cMapPort.LMapTimeResolve(range, Math.Clamp(x / width, 0, 1));

    private double CMapSpoolResolve(LSpool range, TimeSpan time, double width) =>
        Math.Clamp(_cMapPort.LMapRatioResolve(range, time), 0, 1) * width;

    private CMapHit CMapHitResolve(double x, double y, double width, double height)
    {
        if (_cMapFlow.CFlowRange is not { } range || width <= 0)
        {
            return CMapHit.CMapHitNone;
        }

        (TimeSpan origin, TimeSpan limit, _) = _cMapPort.LMapRangeRead(range);
        double originX = CMapSpoolResolve(range, origin, width);
        double limitX = CMapSpoolResolve(range, limit, width);
        double bodyLeft = Math.Min(originX, limitX);
        double bodyWidth = Math.Max(0, Math.Max(originX, limitX) - bodyLeft);
        double coverageTop = Math.Max(0, Math.Max(0, height - 1) - CMapCoverageHeight);
        double railBottom = Math.Max(CMapRailTop, coverageTop - 2);
        double handleWidth = Math.Min(CMapHandleWidth, bodyWidth);
        bool insideY = y >= CMapRailTop && y <= railBottom;
        if (insideY && x >= bodyLeft && x <= bodyLeft + handleWidth)
        {
            return CMapHit.CMapHitOrigin;
        }

        if (insideY && x >= bodyLeft + bodyWidth - handleWidth && x <= bodyLeft + bodyWidth)
        {
            return CMapHit.CMapHitLimit;
        }

        (double moveLeft, double moveWidth, double moveHeight) =
            CMapMoveResolve(bodyLeft, bodyWidth, railBottom - CMapRailTop, handleWidth, width);
        bool insideMove = moveWidth > 0
            && x >= moveLeft && x <= moveLeft + moveWidth
            && y >= CMapRailTop && y <= CMapRailTop + moveHeight;
        return insideMove ? CMapHit.CMapHitBody : CMapHit.CMapHitCursor;
    }

    private IReadOnlyList<CMapBand> CMapBandsResolve(TimeSpan duration, double width, double railHeight)
    {
        var sections = _cMapPort.LMapSectionsRead(_cMapFlow.CFlowSection.CSectionSegment);
        int? selection = _cMapFlow.CFlowSection.CSectionSelection;
        double durationSeconds = duration.TotalSeconds;
        var bands = new List<CMapBand>(sections.Count);
        double top = CMapRailTop + CMapSectionInset;
        double height = Math.Max(4, railHeight - CMapSectionInset * 2);
        for (int index = 0; index < sections.Count; index++)
        {
            (TimeSpan origin, TimeSpan end, int color, bool hidden) = sections[index];
            if (end <= origin)
            {
                continue;
            }

            double left = Math.Clamp(origin.TotalSeconds / durationSeconds * width, 0, width);
            double right = Math.Clamp(end.TotalSeconds / durationSeconds * width, 0, width);
            bands.Add(new CMapBand(
                left, top, left + Math.Max(1, right - left), top + height, color, $"{index + 1}", !hidden,
                index == selection));
        }

        return bands;
    }

    // Each scanned span as left and width on the coverage strip; a span with no width left is skipped.
    private IReadOnlyList<(double, double)> CMapScansResolve(LSpool range, TimeSpan duration, double width)
    {
        double durationSeconds = duration.TotalSeconds;
        var scans = new List<(double, double)>();
        foreach ((TimeSpan origin, TimeSpan limit) in
            _cMapPort.LMapCoverageRead(_cMapFlow.CFlowKeyframe.CKeyframeRanges, range))
        {
            double left = Math.Clamp(origin.TotalSeconds / durationSeconds * width, 0, width);
            double right = Math.Clamp(limit.TotalSeconds / durationSeconds * width, 0, width);
            double scanWidth = Math.Min(Math.Max(1, right - left), Math.Max(0, width - left));
            if (scanWidth > 0)
            {
                scans.Add((left, scanWidth));
            }
        }

        return scans;
    }
}
