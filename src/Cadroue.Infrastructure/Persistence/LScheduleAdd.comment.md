# LScheduleAdd.cs
Hash: `c337e70175888e8a`

## `public IReadOnlyList<LWorkItem> LScheduleAcceptedAdd(`

Adds only items whose identity the schedule does not already hold.
The accepted list lets the caller act on exactly what entered the queue.
Each accepted item is stamped with this launch's signet, and relayed work with this process.
If one item cannot be filed, every item already filed is removed and nothing is added.
So a submission enters the queue whole or not at all.

## `public bool LScheduleOrderSet(Guid lWorkBatchId, IReadOnlyList<Guid> lWorkIds)`

Reorders a batch's pending items by handing out their existing creation times in the requested order.
The request must name exactly the batch's pending items, else nothing changes.
