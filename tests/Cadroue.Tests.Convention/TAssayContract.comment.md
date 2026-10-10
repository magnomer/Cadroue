# TAssayContract.cs
Hash: `a16dfe4f9e2c1460`

## `public sealed class TAssayContract`

Assays of the dangling and hardwiring scans, run through `TAuditContractWalker.TAuditRun`, the contract walker's real entry.
The source sits at `TAssayTruth.TAssayDriverPath` and is handed through the binder seam, so no file is written.
Each dangling source holds its own `QContract` stand-in, so a call inside, nested in or outside it can be assayed.
Most assays hand no markup, so no ID is declared and every constant ID outside `QContract` is a hit.
The `UndeclaredId` pair hands one markup text under a virtual path through the same seam.
A failing assay exposes a walker bug, never a source to fix.

## `public void AuditContract_ForwardInside_AllowsDangling()`

A forwarding call inside `QContract` is skipped, since the outer call is judged where it is written.

## `public void AuditContract_ForwardOutside_ReportsDangling()`

The same forwarding call from another type is a hit, while the inner one stays skipped.

## `public void AuditContract_NestedForward_ReportsDangling()`

A type nested in `QContract` is another type, so its forwarding call is a hit.

## `public void AuditContract_ConstantInside_AllowsDangling()`

A constant ID inside `QContract` that no surface declares is skipped.

## `public void AuditContract_ConstantOutside_ReportsDangling()`

The same constant from another type is a hit, since the surface declares no such ID.

## `public void AuditContract_UndeclaredId_ReportsDangling()`

A constant ID that the handed surface does not declare is reported.

## `public void AuditContract_DeclaredId_AllowsDangling()`

A constant ID that the handed surface declares as `x:Name` is no hit.
It fails when declared IDs no longer clear a constant ID.

## `public void AuditContract_PackLine_ReportsHardwiring()`

A line holding a pack marker is a hit, read from the handed text rather than from disk.

## `public void AuditContract_PlainLine_AllowsHardwiring()`

The same line without the marker is no hit.

## `public void AuditContract_BuiltUri_ReportsHardwiring()`

A `LoadComponent` URI built from a run-time string is a hit, since it can hide a marker.

## `public void AuditContract_LiteralUri_AllowsHardwiring()`

A URI built from a string literal is left to the line scan, which sees no marker here.

## `private static void TAssayContractCheck(string driver, string kind, TViolation? expected, string? markup = null)`

Holds the hits of `kind` in a source against the expected hit, or against none when `expected` is null.
Path, line, member, kind and reason must all match.

## `private static IReadOnlyList<TViolation> TAssayContractRun(string driver, string? markup)`

Walks the source through the binder seam and returns every hit with a repo-relative path.
A handed markup sits at a virtual `.xaml` path in the Veneer folder and is the walk's only surface.
The walker reads lines from the tree's text, so nothing is written to disk.
