# TAuditContractWalker.cs
Hash: `0da0456f202264dd`

## `internal static class TAuditContractWalker`

Measures how far Deportment stands from the master it must become.
A master never names the Veneer, holds no scaffold, and pulls each part by an ID the Veneer declares.
It counts `Hardwiring`, `Masquerading` and `Dangling` hits for the strict ledger.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> driverPaths, IEnumerable<string> markupPaths)`

Reads the contract IDs from the surface markup, then walks every driver source.
The hardwiring scan reads the raw lines of each tree's text, and the other three scans read the bound syntax.
Lines come from the parsed text, never from disk, so an assay source is read like a tracked file.

## `private static void TAuditHardwiringScan(SyntaxNode root, List<TViolation> violations)`

One hit per line holding a pack marker, named after the first marker found.
The lines are the tree's own source text, the same content the file holds.
String literals are read too, since a pack URI is written as one.

## `private static void TAuditLoadScan(SyntaxNode root, List<TViolation> violations)`

One hit per `Application.LoadComponent` URI argument that is not built from a string literal.
A literal is left to the line scan, which sees any pack marker in it.
A URI built at run time can hide its marker from that line scan, so it is hardwiring.

## `private static void TAuditMasqueradingScan(SyntaxNode root, List<TViolation> violations)`

One hit per type declaration whose base types include a scaffold type.
The walk stops at the first scaffold base, so a window counts once.

## `private static void TAuditDanglingScan(SyntaxNode root, IReadOnlySet<string> ids, List<TViolation> violations)`

Finds every call on the contract type and reads each string argument as an ID.
An ID that is not a constant, or that the markup never declares, is one hit.
A call whose nearest enclosing type declaration is the contract type itself is skipped.
Such a call only forwards its caller's ID, and that outer call is judged where it is written.
The skip compares declared symbols, never method names or forwarded parameters.
So a caller elsewhere that forwards its own parameter into the contract type is still a hit.

## `private static IReadOnlySet<string> TAuditIdRead(IEnumerable<string> markupPaths)`

Every value of a contract ID attribute in the surface markup.
The text comes through `TAuditBinder.TAuditMarkupRead`, so an assay can hand markup like source.
A file that does not parse is skipped here and reported by the reach walk.
