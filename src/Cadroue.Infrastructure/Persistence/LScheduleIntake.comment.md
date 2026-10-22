# LScheduleIntake.cs

## `public IReadOnlyList<LWorkItem> LScheduleAcceptedAdd(`

Adds only items whose identity the schedule does not already hold.
The accepted list lets the caller act on exactly what entered the queue.
Each accepted item is stamped with this launch's signet, and relayed work with this process.
If one item cannot be filed, every item already filed is removed and nothing is added.
So a submission enters the queue whole or not at all.

## `public IReadOnlyList<LWorkItem> LScheduleDeliveredAdd(IReadOnlyList<LWorkItem> lWorkItems)`

Files already-finished derived outputs straight into the Done store as completed work.
Each item is recorded at full progress under its current terminal state.
It keeps the batch and lineage it carries.
So it flows through the existing grouping and relay path like any other finished output.
A signet is stamped only when the item has none.
An item that cannot be filed is skipped and traced, and the others still enter.

## `public bool LScheduleOrderSet(Guid lWorkBatchId, IReadOnlyList<Guid> lWorkIds)`

Reorders a batch's pending items by handing out their existing creation times in the requested order.
The request must name exactly the batch's pending items, each once, else nothing changes.
