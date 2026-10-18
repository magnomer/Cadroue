# QRosterDeferral.cs
Hash: `20c32c1a21f76cfb`

## `public sealed class QRosterDeferral`

Paces detail refreshes so a burst of progress notices costs one redraw.
Its only state is the pending flag between a deferral and its run.

## `public QRosterDeferral(Func<bool> lClosedRead, Action lDetailRaise)`

Reads the owner's closed flag and raises the owner's detail notice through delegates.
So the detail notice keeps one home on the owner.

## `public event Action? QRosterDeferralRaised`

Asks the veneer to run the refresh later at background priority.

## `public void QRosterDeferralRaise()`

Raises the deferral once until the deferred run clears the pending flag.

## `public void QRosterDeferralRun()`

Clears the pending flag and raises the detail notice unless the roster is closed.
