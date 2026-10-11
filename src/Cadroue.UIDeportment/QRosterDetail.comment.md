# QRosterDetail.cs
Hash: `20c32c1a21f76cfb`

## `public sealed class QRosterDetail`

Paces detail refreshes so a burst of progress notices costs one redraw.
Its only state is the pending flag between a deferral and its run.

## `public QRosterDetail(Func<bool> lClosedRead, Action lDetailRaise)`

Reads the owner's closed flag and raises the owner's detail notice through delegates.
So the detail notice keeps one home on the owner.

## `public event Action? LRosterDetailDeferred`

Asks the veneer to run the refresh later at background priority.

## `public void LRosterDeferRaise()`

Raises the deferral once until the deferred run clears the pending flag.

## `public void LRosterDetailRun()`

Clears the pending flag and raises the detail notice unless the roster is closed.
