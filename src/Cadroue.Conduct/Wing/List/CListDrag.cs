using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

// One list's drag: the press a drag may start from, its place, and the paths the last drag carried. CList makes one
// and answers its selection and its locks.
public sealed class CListDrag
{
    private readonly CList _cListDragOwner;
    private bool _cListDragOrigin;
    private double _cListDragX;
    private double _cListDragY;

    internal CListDrag(CList owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _cListDragOwner = owner;
    }

    // The path a drag may start from, from an unlocked press until the drag starts or the button is released.
    public string? CListDragPath { get; private set; }

    // The paths the last drag carried.
    public IReadOnlyList<string> CListDragPaths { get; private set; } = [];

    // A press at (x, y) selects as CListPressSelect does; a drag may start from it unless the pressed path is locked.
    public bool CListPressHandle(string path, bool shift, bool control, double x, double y)
    {
        _cListDragOwner.CListPressSelect(path, shift, control);
        if (_cListDragOwner.CListLockCheck(path))
        {
            return false;
        }

        _cListDragOrigin = true;
        _cListDragX = x;
        _cListDragY = y;
        CListDragPath = path;
        return true;
    }

    // A move to (x, y), at least the minimum away from the press on one axis with the button held, starts the drag:
    // it carries the selection's unlocked paths when the pressed path is selected, else the pressed path alone.
    // Answers whether it carries any.
    public bool CListDragResolve(double x, double y, double minimumX, double minimumY, bool pressed)
    {
        if (!_cListDragOrigin || CListDragPath is not { } dragPath || !pressed)
        {
            return false;
        }

        if (Math.Abs(x - _cListDragX) < minimumX && Math.Abs(y - _cListDragY) < minimumY)
        {
            return false;
        }

        IReadOnlyList<string> dragged = _cListDragOwner.CListSelectionCheck(dragPath)
            ? _cListDragOwner.CListSelectionRead().Where(path => !_cListDragOwner.CListLockCheck(path)).ToArray()
            : [dragPath];
        CListDragPaths = dragged;
        CListDragClear();
        _cListDragOwner.CListPressClear();
        return dragged.Count > 0;
    }

    public void CListReleaseHandle()
    {
        CListDragClear();
        _cListDragOwner.CListReleaseSelect();
    }

    private void CListDragClear()
    {
        _cListDragOrigin = false;
        CListDragPath = null;
    }
}
