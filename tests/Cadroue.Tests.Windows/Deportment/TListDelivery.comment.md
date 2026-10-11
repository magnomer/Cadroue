# TListDelivery.cs
Hash: `9e66445df0f3848c`

## `public sealed class TListDelivery`

Checks the list relay that the cartographer calls to deliver files into other tabs and to lock their sources.
Each test attaches the relay to a real strip with a tab, a docket and an attached workspace.
The Cartographer collection keeps it from running beside tests that share the delivery seams.

## `public void DeliveredAdd_ListsExistingMediaInTheTargetTab_RefusesMissingOrUnknown()`

Only a file that exists reaches the target docket.
An unknown tab id is refused too.
The entry is marked delivered, stays unlocked and keeps the batch id.

## `public void SourceClaim_LocksTheSourceInItsTab_ReleaseUnlocksIt()`

A claim locks only the claimed source in the tab named by the work item.
Releasing the source unlocks it again.

## `public void BatchRemove_DropsDeliveredFiles_UnlocksOrphans()`

Removing a batch deletes the files that were delivered for it.
Sources the batch had locked stay in the list and become unlocked.

## `public void DeliveredRemove_ForcedDropsTheSourceFromItsTab()`

A forced remove drops the work item's source from the tab that owns it.
A work item pointing at an unknown tab changes nothing.
