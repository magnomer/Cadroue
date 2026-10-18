# QRosterPick.cs
Hash: `23527f996d31be62`

## `internal sealed record QRosterPick(Guid QRosterPickCurrent, Guid QRosterPickCard, IReadOnlySet<Guid> QRosterPickIds)`

Holds one whole roster selection: the current step, the selected card and the selected steps.
It is immutable, so each select request replaces it at once and no reader sees a half-written pick.
It may still hold ids whose work has left the roster, and the selection reads hide them.
