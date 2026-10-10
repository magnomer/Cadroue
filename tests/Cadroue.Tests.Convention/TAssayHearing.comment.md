# TAssayHearing.cs
Hash: `7ca66e7cd6ca4cc3`

## `public sealed class TAssayHearing`

Assays of the focus exemption in the misfiring scan.
A focus guard that only drops an event the user did not cause is hearing, not misfiring.
A focus read that chooses between paths, or any other control state, stays a hit.
Each hit-assay has a no-hit twin that differs by one thing.
They run through `TAuditTruthWalker.TAuditRun` with the helpers of `TAssayTruth`.
Body line k is driver line 9 + k.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_FocusReturn_AllowsRequest()`

An early `return` when the box lacks keyboard focus only drops the event.

## `public void AuditTruth_EnabledReturn_ReportsMisfiring()`

The same early `return` on a state other than focus still decides the request.

## `public void AuditTruth_FocusedReturn_ReportsMisfiring()`

An early `return` when the box has focus drops the user's own event, so it decides.

## `public void AuditTruth_FocusGuard_AllowsRequest()`

The repertoire's pattern form tests focus `true` beside a type test, with the request inside.

## `public void AuditTruth_UnfocusedGuard_ReportsMisfiring()`

The same pattern testing focus `false` runs the request only for an event the user did not cause.

## `public void AuditTruth_FocusPlain_AllowsRequest()`

A focus member read directly on the control guards the request.

## `public void AuditTruth_EnabledGuard_ReportsMisfiring()`

The same guard on a state other than focus decides the request.

## `public void AuditTruth_FocusChain_AllowsRequest()`

A focus read joined by `&&` to a part that reads no control is still a focus guard.

## `public void AuditTruth_FocusEither_ReportsMisfiring()`

A focus read joined by `||` lets another value pick the request, so it decides.

## `public void AuditTruth_FocusElse_ReportsMisfiring()`

A focus read with an `else` chooses between two requests, so it stays misfiring.

## `public void AuditTruth_FocusLater_ReportsMisfiring()`

A focus guard followed by a later request chooses between two paths, so it stays misfiring.
