# QSplitTabSweep.cs

## `public sealed class QSplitTabSweep`

The class runs the Smart Split detection for the selected file and applies the combined result to the sections.
It is a sibling of the split tab, which owns it.

## `private delegate Task QSplitSweepStep(IProgress<double> lProgress, CancellationToken lToken);`

One enabled detector scan, closed over its own settings at creation time.
Later inspector edits therefore cannot change a running sweep.

## `public event Action<bool>? QSplitBusyApplied;`

Raised true when a sweep begins and false when it ends, whatever the outcome.
The tab mirrors it into the gate's running state, and this class does not touch the sensor surface.

## `public event Action<double>? QSplitProgressApplied;`

Raises overall progress from 0 to 1, with each step taking an equal share.

## `public event Action<string, string>? QSplitFailRaised;`

Carries a localized title and message for the veneer to show.
It is raised for a detection fault or for sections that exceed the ceiling.

## `public bool QSplitSweepRunning => qSplitSweepSource is not null;`

True from the start until the cleanup, the same window in which a second start is refused.

## `public void QSplitSweepCancel() => qSplitSweepSource?.Cancel();`

Cancellation ends the sweep quietly.
The gate applies the combined result only after the scan returns, so a cancelled scan applies nothing.
Test `SweepCancel_Idle_IsNoop` pins the idle case.
Test `SweepButton_RunThenStop_RequestsOnceAndSynchronizesState` pins run then stop.

## `public async Task QSplitSweepStart()`

A start needs an unlocked selection, a loaded timeline and at least one enabled step.
Tests `SweepStart_NoSelection_DoesNotRun`, `SweepStart_NoSpool_DoesNotRun` and `SweepStart_NothingEnabled_DoesNotRun` pin the refusals.
Steps run one after another, and the combined result is applied only after all finish.
The section gate turns editing off for the scan and restores it by the lock state afterward.
Cancellation is silent, and an expected fault is traced and reported.

## `private IReadOnlyList<QSplitSweepStep> QSplitStepsCreate(`

One step is built per enabled detector, and the shared excluded, kept and boundary lists are filled.
Blank, silence and the still scan in the discard mode produce excluded spans.
The still scan in the treat mode produces kept spans.
Scene, luminance and volume produce cut boundaries.
Only scene boundaries carry a minimum length.
Test `StepsRead_EnabledKindsInCatalogOrder` pins the order.
