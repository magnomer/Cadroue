# LRoster.cs
Hash: `376019ca11869dbb`

## `public LRoster(LScheduleContract lScheduleOwner, LStation lStationOwner)`

Builds the collapse, detail-pacing and selection siblings before it subscribes to the schedule.
The collapse sibling rebuilds one card through the owner, so card state stays here.
The detail sibling reads the closed flag and raises the detail notice through owner delegates.
The selection sibling reads which steps and batches are present through owner delegates.
Its one notice runs the owner's card update, detail notice and station notice in that order.

## `public void LRosterClose()`

Runs once, so a second close never closes the station twice.

## `private void LRosterItemHandle(LWorkItem lWorkItem, LScheduleNotice lNotice)`

A status change can alter visibility and batch completion, so it rebuilds every card.
Any other change redraws only the card of the step's batch.
Progress of the shown step goes through the detail pacing, so a burst costs one redraw.

## `public void LRosterElapsedTick(bool lVisible)`

A running job has no finish time yet, so its elapsed figure is measured against the clock.
The shown item's source figures land later, from the low-priority background measurement.
This one-second tick refreshes the detail while any job runs.
Then elapsed and the batch's summed spent/speed advance in real time.
It also refreshes while the shown item or batch is still awaiting measurement.
Then "Measuring" turns into values as soon as the result lands.

## `public void LRosterRebuild()`

Refreshes the present step set the selection sibling reads, but never writes selection.
A schedule change therefore leaves stale ids in place and the selection reads hide them.
No selection notice fires here, because the selection itself did not change.

## `private void LRosterCompletedSync(IReadOnlyList<LWorkItem> lItems)`

A batch counts as completed once none of its steps is pending or running.
With the collapse-done preference on, completed batches collapse and unfinished batches expand.

## `private void LRosterEntriesInsert(int lCardIndex, LRosterCard lCard)`

Finds the flat entry offset from the per-card entry counts, so one card changes without a full reset.

## `private bool LRosterVisibleCheck(LWorkItem lWorkItem)`

Hides a running step only while another live owner runs it.
A running step whose owner is gone stays visible.

## `public void LRosterRemove(Guid lBatchId)`

Offers only the steps the schedule allows to remove, and asks nothing when none qualify.

## `public bool LRosterSharedSet(bool? lChecked)`

A real change reloads the schedule, because the shared option decides which work it loads.

## `public bool LRosterDoneSet(bool? lChecked)`

Turning the option on rebuilds so completed batches collapse at once.
Turning it off leaves the current collapse state as the user sees it.
