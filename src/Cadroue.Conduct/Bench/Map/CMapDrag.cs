using System;
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

// The map's pointer: what a point over the rail grabs, and the drag a press runs on the range or the cursor. CMap
// makes one and answers where a time sits on the rail; the driver hands it raw pointer input and the surface's size.
public sealed class CMapDrag
{
    private readonly LMapPort _cMapDragPort;
    private readonly CMap _cMapDragOwner;
    private readonly CFlow _cMapDragFlow;
    private CMapHit _cMapDragKind;
    private CMapHit _cMapHoverKind;
    private double _cMapDragOrigin;
    private double _cMapDragPrevious;
    private TimeSpan _cMapDragTime;

    internal CMapDrag(LMapPort port, CMap owner, CFlow flow)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(flow);
        _cMapDragPort = port;
        _cMapDragOwner = owner;
        _cMapDragFlow = flow;
    }

    // Told at a drag's start and at its end, for the driver to hand on with the viewer's playing seam.
    public event Action<bool>? CMapDragChanged;

    // A drag edited the range.
    public event Action? CMapSpoolChanged;

    public CMapHit CMapDragKind => _cMapDragKind;

    public CMapHit CMapHoverResolve(double x, double y, double width, double height)
    {
        _cMapHoverKind = CMapHitResolve(x, y, width, height);
        return _cMapHoverKind;
    }

    // Leaving keeps what a drag still grabs; with no drag nothing is grabbed.
    public CMapHit CMapLeaveResolve() => _cMapDragKind == CMapHit.CMapHitNone ? CMapHit.CMapHitNone : _cMapHoverKind;

    public bool CMapPressHandle(double x, double y, double width, double height)
    {
        if (_cMapDragFlow.CFlowRange is not { } range || width <= 0)
        {
            return false;
        }

        CMapDragStart(x, y, width, height);
        CMapPressDispatch(range, x, width);
        return true;
    }

    public bool CMapMoveHandle(double x, double width)
    {
        if (_cMapDragFlow.CFlowRange is not { } range || !CMapDragCheck() || width <= 0)
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
                _cMapDragTime = _cMapDragPort.LMapRangeRead(range).LMapOrigin;
                break;
            case CMapHit.CMapHitLimit:
                _cMapDragTime = _cMapDragPort.LMapRangeRead(range).LMapLimit;
                break;
        }

        CMapDragChanged?.Invoke(true);
        if (_cMapDragKind == CMapHit.CMapHitCursor)
        {
            _cMapDragFlow.CFlowCursorSeek(CMapTimeResolve(range, x, width));
        }
    }

    private void CMapMoveDispatch(LSpool range, double x, double width)
    {
        TimeSpan dragDelta = _cMapDragPort.LMapTimeResolve(range, (x - _cMapDragOrigin) / width);
        switch (_cMapDragKind)
        {
            case CMapHit.CMapHitOrigin:
                _cMapDragPort.LMapOriginSet(range, _cMapDragTime + dragDelta);
                CMapSpoolChanged?.Invoke();
                break;
            case CMapHit.CMapHitLimit:
                _cMapDragPort.LMapLimitSet(range, _cMapDragTime + dragDelta);
                CMapSpoolChanged?.Invoke();
                break;
            case CMapHit.CMapHitBody:
                _cMapDragPort.LMapBodyMove(
                    range, _cMapDragPort.LMapTimeResolve(range, (x - _cMapDragPrevious) / width));
                _cMapDragPrevious = x;
                CMapSpoolChanged?.Invoke();
                break;
            case CMapHit.CMapHitCursor:
                _cMapDragFlow.CFlowCursorSeek(CMapTimeResolve(range, x, width));
                break;
        }
    }

    private bool CMapDragCheck() => _cMapDragKind != CMapHit.CMapHitNone;

    private TimeSpan CMapTimeResolve(LSpool range, double x, double width) =>
        _cMapDragPort.LMapTimeResolve(range, Math.Clamp(x / width, 0, 1));

    private CMapHit CMapHitResolve(double x, double y, double width, double height)
    {
        if (_cMapDragFlow.CFlowRange is not { } range || width <= 0)
        {
            return CMapHit.CMapHitNone;
        }

        (TimeSpan origin, TimeSpan limit, _) = _cMapDragPort.LMapRangeRead(range);
        double originX = _cMapDragOwner.CMapSpoolResolve(range, origin, width);
        double limitX = _cMapDragOwner.CMapSpoolResolve(range, limit, width);
        double bodyLeft = Math.Min(originX, limitX);
        double bodyWidth = Math.Max(0, Math.Max(originX, limitX) - bodyLeft);
        (_, double railBottom) = CMap.CMapRailResolve(height);
        double handleWidth = Math.Min(CMap.CMapHandleWidth, bodyWidth);
        bool insideY = y >= CMap.CMapRailTop && y <= railBottom;
        if (insideY && x >= bodyLeft && x <= bodyLeft + handleWidth)
        {
            return CMapHit.CMapHitOrigin;
        }

        if (insideY && x >= bodyLeft + bodyWidth - handleWidth && x <= bodyLeft + bodyWidth)
        {
            return CMapHit.CMapHitLimit;
        }

        (double moveLeft, double moveWidth, double moveHeight) =
            CMap.CMapMoveResolve(bodyLeft, bodyWidth, railBottom - CMap.CMapRailTop, handleWidth, width);
        bool insideMove = moveWidth > 0
            && x >= moveLeft && x <= moveLeft + moveWidth
            && y >= CMap.CMapRailTop && y <= CMap.CMapRailTop + moveHeight;
        return insideMove ? CMapHit.CMapHitBody : CMapHit.CMapHitCursor;
    }
}
