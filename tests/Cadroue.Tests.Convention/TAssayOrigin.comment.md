# TAssayOrigin.cs
Hash: `03c8779eb874bb1b`

## `public sealed class TAssayOrigin`

Assays of the contesting origin paths that `TAssayContesting` leaves out.
They share its shape.
The driver field is written by the engine and then through one origin path.
Each path is judged as a hit beside a no-hit.
The blank no-hit also has `false` and zero hit twins that guard against over-breadth.
Each rule was switched off once, and only its own facts failed.
A failing assay exposes a walker bug, never a source to fix.

## `private const string TAssayCapsulePath = "src/Cadroue.UIDeportment.Capsule/Assay/LAssayCapsule.cs";`

Where an assay Capsule type lives, under the Capsule include.

## `private const string TAssayCapsuleText = """`

A Capsule type whose read answers by key.

## `public void AuditTruth_PlainDependency_ReportsContesting()`

A dependency property with one plain `SetValue` beside an engine one is plain, so its copy contests.

## `public void AuditTruth_EngineDependency_AllowsEngineCopy()`

The same dependency property with two engine `SetValue` calls is engine.

## `public void AuditTruth_PlainGetter_ReportsContesting()`

A getter body that returns a plain holder is plain.

## `public void AuditTruth_EngineGetter_AllowsEngineCopy()`

The same getter returning a holder started from an engine read is engine.

## `public void AuditTruth_PlainParameter_ReportsContesting()`

A method parameter takes every call's argument, so one literal argument makes it plain.

## `public void AuditTruth_EngineParameter_AllowsEngineCopy()`

The same parameter with only engine arguments is engine.

## `public void AuditTruth_GroupParameter_ReportsContesting()`

A method also used as a method group has unseen callers, so its parameter stays plain.

## `public void AuditTruth_BlankWriter_AllowsEngineCopy()`

Writes of `string.Empty` and `""` beside an engine write are clears, so nothing contests.

## `public void AuditTruth_ZeroWriter_ReportsContesting()`

A zero write beside an engine write stays plain, so the blank rule stays narrow.

## `public void AuditTruth_FalseWriter_ReportsContesting()`

A `false` write beside an engine write stays plain too.

## `public void AuditTruth_EngineKey_AllowsCapsuleRead()`

A Capsule read keyed by an engine value is plain, so it does not contest a plain write.

## `public void AuditTruth_CapsuleRead_ReportsContesting()`

A Capsule read beside a real engine writer still contests.
With the Capsule rule off the read turns engine, so this fact fails as well.

## `private static List<TViolation> TAssayCapsuleRun(string body)`

Walks a driver holding the assay Capsule beside its body, and keeps the contesting hits.
