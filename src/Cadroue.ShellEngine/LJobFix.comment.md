# LJobFix.cs
Hash: `2ec203aef0f86e14`

## `private const int LJobPassMax = 2;`

The budget covers the initial Fix repair pass and any recomposed pass over the same source.
A repair can resolve a defect a lower-precedence repair was also going to address.
It can also expose one the first scan could not see past the first defect.
A pass that does not validate as Done is rescanned only while the pass budget remains.
Correctable selected defects can then form another repair pass.
That is bounded here so a defect that never clears cannot loop forever.

## `private async Task LJobDossiersCreate()`

Dossiers already on the item win, then the cached checkup, and only then a fresh scan.
The cache comes through LCheckupEngine, so the job never names the Infrastructure checkup.
A fresh scan is saved back to that cache, so a later run of the same source reuses it.

## `private async Task<(int, string)> LJobRepairRun(LEncodeStage pStage, int pStageNumber, int pStageCount)`

Each repair writes a temporary file beside the output and then moves it over the output.
So a failed repair stage leaves the previous output intact.

## `private async Task<(int, string)> LJobValidateRun(LEncodeStage pStage, int pStageNumber, int pStageCount)`

Validation always returns success, and its verdict lives in the validate state the outcome reads.
The validation probe receives the runner's priority override, keeping its measurement under the same runner configuration.

## Inline notes

### `LWorkFixSalvage pSalvage = lJobItem.LWorkFixPlan.LWorkFixSalvage;`

Salvage follows the repair passes when its policy allows it.
Recovery accepts an extracted output only after a clean process exit, nonempty file and readable probe.
Temporary cleanup retries failed deletions, but a persistent failure can leave a temporary file behind.
The recovered paths are held for the terminal outcome to record as delivered derived outputs (`LJobSalvageRecord`).
What it reads and whether it runs depend on the plan.
- No repair step selected: salvage is the only work, always run from the source.
- From source: recover from the original source, but only when the repair did not fully succeed.
  Any state other than Done counts as failed.
- From fixed result: always recover, reading the repaired output.
  That falls back to the source when the repair produced no output.

### `if (lJobValidateState == LWorkState.LWorkStateDone || pPass + 1 >= LJobPassMax)`

Validation cleared the file, or the pass budget is spent.
Stop here and let the final validation state stand as this job's outcome.

### `IReadOnlyList<LDossier> pRescan`

Re-scan the repaired output and keep only the correctable repairs the user asked for.
A report-only FFV1 dossier is never re-run.

### `var pRemainingKinds = pRemaining.Select(pDossier => pDossier.LDossierKind).ToHashSet();`

The first remaining set may trigger recomposition without comparison to the original repair set.
Only subsequent remaining sets are compared, and the two-pass budget stops before another comparison can occur.

### `&& pRepairable.Any(pDossier => pDossier.LDossierRepair == LFlawFfvone.LFlawReport))`

A selected report-only FFV1 slice-CRC mismatch remains unresolved even when the output passes probing and decoding.
Never let it read as resolved.
The report-only override considers only defects the plan selected for repair.
Other detected defects do not trigger that override, but whole-output probe and decode failures still gate validation.
