# TAssayMisfiring.cs
Hash: `95f6de869d00617b`

## `public sealed class TAssayMisfiring`

Assays of the misfiring scan on control reads in `switch`, `if` and ternary forms.
The control is a local `TextBox`, since the walker knows controls by their framework base.
Each hit-assay has a no-hit twin that differs by one thing.
They run through `TAuditTruthWalker.TAuditRun` with the helpers of `TAssayTruth`.
Body line k is driver line 9 + k.
The local is on line 10 and the next statement on line 11.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_SwitchGuardControl_ReportsMisfiring()`

A `when` guard that reads a control and picks a request decides the request.
The hit sits on the switch section.

## `public void AuditTruth_SwitchGuardFlag_AllowsRequest()`

The same switch with a guard that reads no control decides nothing.

## `public void AuditTruth_SwitchConstantLabel_ReportsMisfiring()`

A constant label on a switch governed by a control read picks the request by the control's value.

## `public void AuditTruth_SwitchTypeLabel_AllowsRequest()`

A type-only label on a control's `DataContext` is pairing, not a control decision.

## `public void AuditTruth_SwitchNestedConstant_ReportsMisfiring()`

A property pattern that holds a value tests the control's state, so it decides.

## `public void AuditTruth_SwitchNestedType_AllowsRequest()`

A property pattern of type tests alone is pairing, so it decides nothing.

## `public void AuditTruth_GuardNestedType_AllowsRequest()`

A guard that is one `is` pattern of type tests alone is pairing, so the section decides nothing.
Its twin `GuardNestedConstant` fails when the guard rule is removed.

## `public void AuditTruth_IfNestedConstant_ReportsMisfiring()`

An `if` on a control read through a value-holding property pattern decides the request.

## `public void AuditTruth_IfNestedType_AllowsRequest()`

An `if` on a control read through a type-only property pattern is pairing, as in a switch.

## `public void AuditTruth_TernaryNestedConstant_ReportsMisfiring()`

A ternary on a control read through a value-holding property pattern decides the request.

## `public void AuditTruth_TernaryNestedType_AllowsRequest()`

A ternary on a control read through a type-only property pattern is pairing, as in a switch.

## `public void AuditTruth_GuardNestedConstant_ReportsMisfiring()`

This is the hit twin of `GuardNestedType`.
A guard testing the control's state through a nested pattern decides.
It fails when the guard rule is removed, which the no-hit twin alone cannot show.

## `public void AuditTruth_ArmConstant_ReportsMisfiring()`

A switch-expression arm with a constant on a control read picks the request.
The hit sits on the arm, and the discard arm beside it adds none.

## `public void AuditTruth_ArmType_AllowsRequest()`

An arm of a type test on the control's `DataContext` is pairing.

## `public void AuditTruth_ArmDiscard_AllowsRequest()`

A lone discard arm tests no value, so the control read decides nothing.

## `public void AuditTruth_SwitchDefault_AllowsRequest()`

A `default` label tests no value, so the twin of `SwitchConstantLabel` decides nothing.

## `public void AuditTruth_RelationalLabel_ReportsMisfiring()`

A relational label on a control read tests its value, so it decides.

## `public void AuditTruth_RelationalPlain_AllowsRequest()`

The same label on a governing expression that reads no control decides nothing.

## `public void AuditTruth_GuardValue_ReportsMisfiring()`

A `when` guard comparing a control's value with a constant decides.

## `public void AuditTruth_GuardPresence_AllowsRequest()`

A `when` guard comparing a control's value with `null` is a presence test.

## `public void AuditTruth_TernaryValue_ReportsMisfiring()`

A ternary comparing a control's value with a constant decides the request.

## `public void AuditTruth_TernaryPresence_AllowsRequest()`

A ternary comparing a control's value with `null` is a presence test.

## `public void AuditTruth_RelationalIf_ReportsMisfiring()`

An `if` on a relational pattern over a control read tests its value, so it decides.
It fails when a relational pattern counts as pairing.

## `public void AuditTruth_RelationalType_AllowsRequest()`

The same `if` with a type-only pattern on the control's `DataContext` is pairing.
No switch-off turns it red, so it guards only against over-breadth.

## `public void AuditTruth_RelationalArm_ReportsMisfiring()`

A relational arm on a control read picks the request, as a relational label does.
It fails when a relational pattern counts as pairing.

## `public void AuditTruth_DefaultControl_ReportsMisfiring()`

The control decides whether the `default` section runs, so the switch decides its request.
The hit sits on the `case "a"` section, whose label reads the control.
It fails when only the section's own request counts.

## `public void AuditTruth_DiscardControl_ReportsMisfiring()`

The control decides whether the discard arm runs, so the switch decides its request.
The hit sits on the `"a"` arm, whose pattern reads the control.
It fails when only the arm's own expression counts.
