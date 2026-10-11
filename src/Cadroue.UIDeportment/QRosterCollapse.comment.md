# QRosterCollapse.cs
Hash: `f1740e87cba109a9`

## `public sealed class QRosterCollapse`

Owns which batch cards are collapsed in the roster, apart from selection and the card list.
Its only state is the collapsed batch set, which no other roster role writes.

## `public QRosterCollapse(Action<Guid> lCardUpdate)`

Takes the owner's one-card rebuild as a delegate, so a toggle redraws only its card.

## `public bool LRosterCollapsedCheck(Guid lBatchId)`

Reports whether a batch card shows only its title row.

## `public void LRosterCollapseSet(Guid lBatchId, bool lCollapsed)`

Sets one batch's collapsed state without redrawing, for the owner's completed-batch sync.

## `public bool LRosterCollapseToggle(Guid lBatchId)`

Flips one batch, rebuilds its card and returns the new state for the caller.

## `public void LRosterStaleRemove(IReadOnlyCollection<Guid> lBatchIds)`

Drops batches that left the roster, so a returning batch id never starts collapsed.
