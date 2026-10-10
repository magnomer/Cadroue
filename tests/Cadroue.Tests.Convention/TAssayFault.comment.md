# TAssayFault.cs
Hash: `dadd2f97072dbc52`

## `public sealed class TAssayFault`

Assays of the fault walker: one hand-written catch clause with an expected hit or no hit.
Each assay binds through `TAuditBinder` and runs `TAuditFaultWalker.TAuditFaultScan`, the real entry of the walker.
Assays check the walker, not the tree, so they gate no tree result and touch no ceiling.
A failing assay exposes a walker bug, never a source to fix.
`TAuditFault` holds the facts over the tree.

## `internal const string TAssayFaultPath = "src/Cadroue.Application/Assay/LAssay.cs";`

The virtual path of the assayed source, under the Application ring so the walker reads it.

## `public void AuditFault_InterfaceRecord_ReportsSwallowing()`

Recording the fault through an interface keeps it from the user, so the clause is a hit.

## `public void AuditFault_NullReturn_ReportsSwallowing()`

Turning the fault into null drops it, so the clause is a hit.

## `public void AuditFault_Rethrow_AllowsCatch()`

A rethrow hands the fault out, even after recording it.

## `public void AuditFault_OutParameter_AllowsCatch()`

The fault assigned to an `out` parameter reaches the caller.

## `public void AuditFault_EventInvoke_AllowsCatch()`

Raising an event tells a listener, even when the fault itself stays behind.

## `public void AuditFault_Cancellation_AllowsCatch()`

A cancellation is exempt by shape, even with an empty block.

## `public void AuditFault_FieldHelper_AllowsCatch()`

A fault passed to a ring helper that stores it in a field is carried out through the helper.

## `internal static void TAssayFaultCheck(string clause, TViolation? expected)`

Runs the walker over one assayed source and expects exactly the one hit, or none.

## `internal static string TAssayFaultFormat(string clause)`

The assayed source with the clause after a `try` in `LAssayRun`.
The clause always opens on line 30, so an expected hit names that line.
`LAssayRecord` is an interface, so a call through it is a recording, never a way out.
`LAssayKeep` stores its argument in a field, so its parameter is carried.
