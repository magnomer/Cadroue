# LJobFix.cs

## `private const int LJobPassMax = 2`

Total Fix repair passes over one source: the initial pass plus recompose passes.
A repair can resolve a defect a lower-precedence repair was also going to address.
It can also expose one the first scan could not see past the first defect.
So after each pass the output is re-scanned and the still-warranted repairs are recomposed and run again.
That is bounded here so a defect that never clears cannot loop forever.

## Inline notes

### `LWorkFixSalvage pSalvage = lJobItem.LWorkFixPlan.LWorkFixSalvage`

Salvage is the last pass.
It harvests the readable spans and extracts each as a valid standalone file.
It fails safe so nothing partial is left behind.
The recovered paths are held for the terminal outcome to record as delivered derived outputs (`LJobSalvageRecord`).
What it reads and whether it runs depend on the plan.
- No repair step selected: salvage is the only work, always run from the source.
- From source: recover from the original source, but only when the repair did not fully succeed.
- Any state other than Done counts as failed.
- From fixed result: always recover, reading the repaired output.
- That falls back to the source when the repair produced no output.

### `if (lJobValidateState == LWorkState.LWorkStateDone || pPass + 1 >= LJobPassMax)`

Validation cleared the file, or the pass budget is spent.
Stop here and let the final validation state stand as this job's outcome.

### `IReadOnlyList<LDossier> pRescan`

Re-scan the repaired output and keep only the correctable repairs the user asked for.
A report-only FFV1 dossier is never re-run.

### `var pRemainingKinds = pRemaining.Select(pDossier => pDossier.LDossierKind).ToHashSet()`

Only recompose when the correctable set actually changed.
An unchanged set would repeat the same repairs to the same effect and never converge.

### `IReadOnlyList<LDossier> pRepairable`

A report-only defect (FFV1 slice-CRC mismatch) cannot be corrected: the output is a faithful copy, not a repair.
Never let it read as resolved.
Only defects the plan asked to repair gate the outcome.
A detected defect the user left unselected is out of this job's scope.
