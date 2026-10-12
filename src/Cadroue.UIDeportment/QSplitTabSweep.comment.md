# QSplitTabSweep.cs
Hash: `c7b5099229534b62`

## `public sealed class QSplitTabSweep`

Runs the Smart Split detection for the selected file and applies the combined result to the sections.
It is a sibling of the split tab, which owns it.

## `private delegate Task QSplitSweepStep(IProgress<double> lProgress, CancellationToken lToken);`

One enabled detector scan, closed over its own settings at creation time.
Later inspector edits therefore cannot change a running sweep.

## `public event Action<bool>? QSplitBusyApplied;`

Raised true when a sweep begins and false when it ends, whatever the outcome.

## `public event Action<double>? QSplitProgressApplied;`

Raises overall progress from 0 to 1, with each step taking an equal share.

## `public event Action<string, string>? QSplitFailRaised;`

Carries a localized title and message for the veneer to show.
It is raised for a detection fault or for sections that exceed the ceiling.

## `public bool QSplitSweepRunning => qSplitSweepSource is not null;`

True from the start until the cleanup, the same window in which a second start is refused.

## `public IReadOnlyList<LDetectorKind> QSplitStepsRead()`

Lists the enabled detector kinds in catalog order.
The blank detector is enabled through its own section, not the sensor.

## `public void QSplitSweepCancel() => qSplitSweepSource?.Cancel();`

Cancellation ends the sweep quietly and keeps the sections untouched.

## `public async Task QSplitSweepStart()`

Needs an unlocked selection, a loaded timeline and at least one enabled step.
Steps run one after another, and the combined result is applied only after all finish.
Editing is locked and the sensor shows running until the cleanup restores both.
Cancellation is silent, and an expected fault is traced and reported.

## `private IReadOnlyList<QSplitSweepStep> QSplitStepsCreate(`

Builds one step per enabled detector and fills the shared excluded, kept and boundary lists.
Blank, silence and the still scan in the discard mode produce excluded spans.
The still scan in the treat mode produces kept spans.
Scene, luminance and volume produce cut boundaries.
Only scene boundaries carry a minimum length.

## `private bool QSplitLockedCheck() =>`

Whether the current file is locked in the docket.

## `private LDocketEntry? QSplitSelectedRead() =>`

The current entry only if it exists and is unlocked.
