# QFlowSpool.cs
Hash: `34413747728a83b8`

## `public sealed class QFlowSpool`

Owns the working-range role of the timeline: range set, range notice, drag anchor and labels.
The spool itself stays on the owner, because cursor, wheel and shortcut roles read it too.

## `public QFlowSpool(QFlow lOwner)`

Holds the owner to read its spool and to reach its keyframe sibling.

## `public event Action? QFlowSpoolApplied`

Raised on every range change so both strips and the labels redraw.

## `public TimeSpan QFlowDragTime`

The range edge time a map grip drag started from.

## `public string QFlowLabelOrigin`

The working range start, formatted for the viewfinder label.

## `public string QFlowLabelLimit`

The working range end, formatted for the viewfinder label.

## `public string QFlowLabelZero`

The media start, formatted for the map label.

## `public string QFlowLabelDuration`

The media duration, formatted for the map label.

## `public void QFlowDragSet(TimeSpan lDragTime)`

Records the grip drag anchor when a map drag starts.

## `public void QFlowRangeSet(TimeSpan lOrigin, TimeSpan lLimit)`

Sets the working range when media is loaded, else does nothing.

## `public void QFlowSpoolHandle()`

Suspends the keyframe scan before the range notice, so a drag never starts a scan per step.

## `public void QFlowSpoolUpdate()`

Raises the range notice, then defers a keyframe scan only while media is loaded.
