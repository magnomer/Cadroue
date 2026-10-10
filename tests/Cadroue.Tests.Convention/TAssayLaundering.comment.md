# TAssayLaundering.cs
Hash: `31e324cbd6e82db2`

## `public sealed class TAssayLaundering`

Assays of the laundering walk, run through `TAuditLaunderingWalker.TAuditRun`, the walker's real entry.
The readers come from `TAuditTruthWalker.TAuditReaderRead`, as in the tracked audit.
Each assay is a hit and no-hit pair that differs in one operand.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_FlagEquals_ReportsLaundering()`

A logic bool compared with another value is a laundering decision.

## `public void AuditTruth_TrueEquals_AllowsComparison()`

A logic bool compared with `true` or `false` is no laundering decision.
It fails when the literal comparison is no longer skipped.

## `public void AuditTruth_StaticProperty_ReportsLaundering()`

A static property of a Conduct type is a logic source.
It fails when static Conduct members are no longer read as sources.

## `public void AuditTruth_EnumMember_AllowsComparison()`

An enum member of a Conduct type is no logic source.
It fails when enum members are no longer excluded.

## `public void AuditTruth_MixedChain_ReportsLaundering()`

A chain that joins a presence test with a plain operand decides an if.

## `public void AuditTruth_PresenceChain_AllowsDecision()`

A chain of presence tests joined by `&&` is presence, so it decides nothing.
It fails when chains are no longer split into operands.

## `public void AuditTruth_PlainOperand_ReportsLaundering()`

A logic value read inside an interpolation is still its source.

## `public void AuditTruth_NameofOperand_AllowsComparison()`

A logic local named inside `nameof` is not read.
It fails when `nameof` operands are no longer skipped.

## `public void AuditTruth_RunText_AllowsDecision()`

The `Text` of a `Run` deciding an if is no control input.
It fails when an input member is coloured by its name alone.

## `public void AuditTruth_TextBlockText_AllowsDecision()`

The `Text` of a `TextBlock` deciding an if is no control input.
A `TextBlock` is a `FrameworkElement` but no `Control`, so the user cannot change it.

## `public void AuditTruth_TextBoxText_ReportsLaundering()`

The `Text` of a `TextBox` deciding an if is control input.
It fails when a control that takes input is no longer coloured.

## `public void AuditTruth_ToggleChecked_ReportsLaundering()`

The `IsChecked` of a `ToggleButton` deciding an if is control input.
It fails when the base walk checks only the direct base type.

## `public void AuditTruth_UnresolvedReceiver_ReportsLaundering()`

An input member on a `dynamic` receiver keeps its colour, even when the object is a `Run`.
The assay compilation rejects errors, so `dynamic` stands in for a receiver type the model cannot resolve.
It fails when an unresolved read escapes the rule.

## `private static void TAssayLaunderingCheck(string body, TViolation? expected)`

Holds the hits of a driver body against the expected hit, or against none when `expected` is null.
The driver carries two reader members that read the gate, so their results are logic values.
The gate is this file's own Conduct source, with a static property and an enum beside `CAssayRead`.
Path, line, member, kind and reason must all match.
