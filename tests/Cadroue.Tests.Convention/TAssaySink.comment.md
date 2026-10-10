# TAssaySink.cs
Hash: `cf2cd066fe22bd29`

## `public sealed class TAssaySink`

Assays of the sink half of the driver walker, run through `TAuditTruthWalker.TAuditRun` as `TAssayTruth` does.
They share its driver path, helpers and stand-in gate.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditTruth_SwitchBreakAfter_ReportsGatekeeping()`

A `break` in a branch leaves its switch section, so a request after the branch in that section is gated.
The hit sits on the field read in the branch condition, driver line 15.

## `public void AuditTruth_SwitchBreakLater_AllowsRequest()`

A request only in a later case is not skipped by the `break`, so the branch gates nothing.

## `public void AuditTruth_LoopContinueInside_ReportsGatekeeping()`

A `continue` in a branch skips the rest of its loop, so a later request in that loop is gated.

## `public void AuditTruth_LoopContinueAfter_AllowsRequest()`

A `continue` scopes to its loop, so a request after the loop is not gated by it.

## `public void AuditTruth_MixedChain_ReportsGatekeeping()`

A condition chaining a presence test with a value test decides the request.

## `public void AuditTruth_PresenceChain_AllowsRequest()`

A chain of presence tests alone is still a presence check, so it gates nothing.

## `public void AuditTruth_ForgivenOperand_ReportsGatekeeping()`

A field read through parentheses and `!` and compared as a value decides the request.

## `public void AuditTruth_ForgivenReceiver_AllowsRequest()`

The same field wrapped in parentheses and `!` is still a receiver, so reading its member is no sink.

## `public void AuditTruth_PlainField_ReportsGatekeeping()`

A field read in a branch condition decides the request.

## `public void AuditTruth_NameofField_AllowsRequest()`

A field named only inside `nameof` is not read, so the branch reads no field.

## `public void AuditTruth_RefParameter_ReportsGatekeeping()`

A field passed by `ref` is followed into the callee's parameter.
The hit sits on the parameter read in the callee's branch, reported through the ref.

## `public void AuditTruth_ValueParameter_AllowsRequest()`

A field passed by value is not followed, so the callee's branch reads no field.
