# QSummary.cs
Hash: `8d0e6c06dfa087b3`

## `public static string? QSummaryMeterFormat(IReadOnlyList<LWorkItem> lBatchItems, long? lOutputBytes)`

Sums the time each job spent, not the wall-clock span of the batch.
No meter shows until some job has spent time.

## `public static (IReadOnlyList<string>, IReadOnlyList<string>) LSummaryPathsRead(`

Lists the batch's true inputs and final outputs, each path once.
A file one job produced and another consumed is an intermediate and appears in neither list.

## `public static (long? lSourceTotal, long? lOutputTotal) LSummarySizeRead(IReadOnlyList<LWorkItem> lBatchItems)`

Totals the same true inputs and final outputs as the path lists.
A merge input takes its own size from the merge byte list, by position.
One unknown input size makes the source total unknown, so the bar never compares a partial sum.

## Inline notes

### `long lOutputTotal = 0`

Only completed outputs are totalled, since a progressing job's growing file must not be accounted.
So the comparison stays source vs finished output and grows as jobs land.
