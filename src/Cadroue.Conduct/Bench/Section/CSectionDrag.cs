using System;
using System.Collections.Generic;

namespace Cadroue.Conduct;

// The section rows' drag: a press readies it, a move past the threshold starts it and moves the row live to the slot
// under the pointer, and a release ends it or, unmoved, selects the row by its modifiers. CSectionList makes one and
// answers the editor and the painted row count; CSection answers the editable switch and the edits the drag runs.
public sealed class CSectionDrag
{
    private readonly CSection _cSectionDragSection;
    private readonly CSectionList _cSectionDragList;
    private bool _cSectionDragActive;
    private bool _cSectionDragOrigin;
    private double _cSectionDragX;
    private double _cSectionDragY;

    internal CSectionDrag(CSection section, CSectionList list)
    {
        ArgumentNullException.ThrowIfNull(section);
        ArgumentNullException.ThrowIfNull(list);
        _cSectionDragSection = section;
        _cSectionDragList = list;
    }

    // Told with the dragged row when a drag starts.
    public event Action<int>? CSectionDragStarted;

    // Told with the dragged row when a readied drag ends, moved or not.
    public event Action<int>? CSectionDragEnd;

    // Told with the row's source and its new slot each time it moves live.
    public event Action<int, int>? CSectionDragMoved;

    public bool CSectionDragActive => _cSectionDragActive;

    // The readied or dragged row; null while no press readies a drag.
    public int? CSectionDragIndex { get; private set; }

    // The dragged row, or -1 while none is readied.
    public int CSectionDragRead() => CSectionDragIndex ?? -1;

    // A single press readies a drag from its row; a double press is not a drag.
    public bool CSectionDragPrepare(int index, int clickCount, double x, double y)
    {
        if (clickCount >= 2)
        {
            return false;
        }

        CSectionDragIndex = index;
        _cSectionDragActive = false;
        _cSectionDragOrigin = true;
        _cSectionDragX = x;
        _cSectionDragY = y;
        return true;
    }

    // A move with the button held, while the owner's switch is editable and no row is edited: past the threshold the
    // drag starts, and the row moves live to the slot under the pointer, found from each painted row's top and height.
    public bool CSectionDragMove(
        double x,
        double y,
        double minimumX,
        double minimumY,
        bool pressed,
        IReadOnlyList<double> tops,
        IReadOnlyList<double> heights)
    {
        bool editable = _cSectionDragSection.CSectionEditable;
        if (!CSectionDragCheck(editable) || !_cSectionDragOrigin || CSectionDragIndex is not int dragIndex || !pressed)
        {
            return false;
        }

        if (!_cSectionDragActive
            && Math.Abs(x - _cSectionDragX) < minimumX
            && Math.Abs(y - _cSectionDragY) < minimumY)
        {
            return false;
        }

        CSectionDragStart(dragIndex);
        CSectionDragUpdate(editable, dragIndex, CSectionDragResolve(y, tops, heights));
        return true;
    }

    // The release: a moved drag rebuilds the rows; an unmoved one commits the pending edit of another row and selects
    // its row by range, by toggle or alone.
    public bool CSectionDragCommit(bool shift, bool control)
    {
        if (!_cSectionDragOrigin || CSectionDragIndex is not int index)
        {
            return false;
        }

        bool moved = _cSectionDragActive;
        CSectionDragEnd?.Invoke(index);
        CSectionDragClear();
        if (moved)
        {
            _cSectionDragList.CSectionRowsRaise();
            return true;
        }

        if (index >= 0 && _cSectionDragList.CSectionEdit.CSectionEditIndex != index)
        {
            _cSectionDragList.CSectionEdit.CSectionEditCommit();
            if (shift)
            {
                _cSectionDragSection.CSectionRangeSelect(index);
            }
            else if (control)
            {
                _cSectionDragSection.CSectionSelectToggle(index);
            }
            else
            {
                _cSectionDragSection.CSectionSelect(index);
            }
        }

        return true;
    }

    // The pointer capture was lost: a readied drag ends without a move.
    public void CSectionDragReset()
    {
        if (CSectionDragIndex is int index)
        {
            CSectionDragEnd?.Invoke(index);
        }

        CSectionDragClear();
    }

    internal void CSectionDragClear()
    {
        CSectionDragIndex = null;
        _cSectionDragActive = false;
        _cSectionDragOrigin = false;
    }

    private bool CSectionDragCheck(bool editable) =>
        editable && CSectionDragIndex is not null && _cSectionDragList.CSectionEdit.CSectionEditIndex is null;

    private void CSectionDragStart(int dragIndex)
    {
        if (_cSectionDragActive)
        {
            return;
        }

        _cSectionDragActive = true;
        CSectionDragStarted?.Invoke(dragIndex);
    }

    // The row goes to the slot before the target; the move the engine refuses leaves the drag where it was.
    private void CSectionDragUpdate(bool editable, int source, int target)
    {
        int clamped = Math.Clamp(target, 0, _cSectionDragList.CSectionRowCount);
        int insert = source < clamped ? clamped - 1 : clamped;
        if (source == insert || !_cSectionDragSection.CSectionMove(editable, source, clamped))
        {
            return;
        }

        CSectionDragIndex = insert;
        CSectionDragMoved?.Invoke(source, insert);
    }

    // The slot under the pointer: past each painted row's middle, the slot after it.
    private int CSectionDragResolve(double y, IReadOnlyList<double> tops, IReadOnlyList<double> heights)
    {
        int target = 0;
        for (int index = 0; index < tops.Count; index++)
        {
            if (y > tops[index] + heights[index] / 2)
            {
                target = index + 1;
            }
        }

        return Math.Clamp(target, 0, _cSectionDragList.CSectionRowCount);
    }
}
