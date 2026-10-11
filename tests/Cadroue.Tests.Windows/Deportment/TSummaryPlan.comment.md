# TSummaryPlan.cs
Hash: `e4924954f2411313`

## `public sealed class TSummaryPlan`

Checks the batch summary plan of the roster without any control.
The plan is read from the job items of one queue card.
The Schedule collection keeps it from running beside tests that share the schedule.

## `public void Files_ReadOneOrMany()`

The file count text differs between one file and several.

## `public void Read_WithoutDoneOutputs_HasNoMeterNoBar_AndListsEveryPath()`

A batch with no finished output has nothing to measure.
It shows no meter and no bar but still lists every source and output path.
Source sizes are summed and the output size reads as unknown.

## `public void Read_WithDoneOutputs_SumsSpentSizesAndSkipsConsumedStages()`

A stage whose output feeds the next stage is not counted as a source or an output of the batch.
Only the first source and the last output are compared.
Spent time is summed over all finished stages.

## `public void Read_IgnoresAnUnfinishedOutput_AndReportsUnknownSourceWhenAnyIsUnmeasured()`

One unmeasured source makes the whole source size unknown.
An output that is still running is not counted.
With nothing finished there is no meter.
