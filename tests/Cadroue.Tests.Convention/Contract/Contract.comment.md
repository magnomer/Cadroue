# Contract

This folder holds the contract walker, which counts how far the driver code stands from its master, and its assay.

## `TAssayContract`

Assays of the dangling and hardwiring scans, run through `TAuditContractWalker.TAuditRun`.
The source sits at `TAssayTruth.TAssayDriverPath` and is handed through the binder seam, so no file is written.
Each dangling source holds its own contract stand-in, so calls inside, nested in or outside it can be assayed.
Most assays hand no markup, so no ID is declared and every constant ID outside the stand-in is a hit.
The `UndeclaredId` pair hands one markup text under a virtual `.xaml` path in the surface folder.
A forwarding call or constant ID inside the stand-in is skipped, since the outer call is judged elsewhere.
The same call from another type, or from a type nested in the stand-in, is a hit.
A constant ID the handed surface declares as `x:Name` is no hit.
A line holding a pack marker is a hit, read from the handed text rather than from disk.
A `LoadComponent` URI built from a run-time string is a hit, since it can hide a marker.
A URI built from a string literal is left to the line scan.
A check holds path, line, member, kind and reason against the expected hit, or against none.

## `TAuditContractWalker`

Measures how far the driver code stands from the master it must become.
A master never names the surface, holds no scaffold, and pulls each part by an ID the surface declares.
It counts `Hardwiring`, `Masquerading` and `Dangling` hits for the strict ledger.
It reads the contract IDs from the surface markup, then walks every driver source.
The text comes through `TAuditBinder.TAuditMarkupRead`, so an assay can hand markup like source.
A markup file that does not parse is skipped here and reported by the reach walk.
The hardwiring scan reads the raw lines of each tree's text, and the other three scans read the bound syntax.
Hardwiring is one hit per line holding a pack marker, named after the first marker found.
String literals are read too, since a pack URI is written as one.
One hit per `Application.LoadComponent` URI argument not built from a string literal is hardwiring too.
A literal is left to the line scan, which sees any pack marker in it.
A URI built at run time can hide its marker from that line scan.
Masquerading is one hit per type declaration whose base types include a scaffold type.
The walk stops at the first scaffold base, so a type counts once.
Dangling finds every call on the contract type and reads each string argument as an ID.
An ID that is not a constant, or that the markup never declares, is one hit.
A call whose nearest enclosing type declaration is the contract type itself is skipped.
Such a call only forwards its caller's ID, and that outer call is judged where it is written.
The skip compares declared symbols, never method names or forwarded parameters.
So a caller elsewhere that forwards its own parameter into the contract type is still a hit.

## Stamp

Commit: `90a13c9590ba`
- `TAssayContract.cs` `243acda031a8dfdb`
- `TAuditContractWalker.cs` `0da0456f202264dd`
