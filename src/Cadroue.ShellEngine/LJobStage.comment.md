# LJobStage.cs
Hash: `11feca0f55c222d7`

## `private async Task<(int, string)> LJobStagesRun()`

`LEncodeSmartCheck` answers whether Smart must be refused, so a false answer takes the Smart path.
A Smart request refused here is recorded with its reason and encodes the full interval.

## `private async Task<(int, string)> LJobBatchRun(`

Each stage waits out a runner pause before it starts.
A stage joins the done list before running, so a failed stage still has its temporary file cleared.
A loudness measuring stage hands its stderr to the next stage carrying the measure token.

## `private async Task<(int, string)> LJobStageRun(`

The runner learns the work id before the employer starts the child, not after.
So a pause that lands during the start already holds the id, and the child starts suspended.
The stage hands the employer only the work id, and the runner forgets it once the stage returns.
