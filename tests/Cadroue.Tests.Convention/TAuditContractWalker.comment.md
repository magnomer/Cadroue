# TAuditContractWalker.cs

## `internal static class TAuditContractWalker`

Measures how far Deportment stands from the master it must become.
A master never names the Veneer, holds no scaffold, and pulls each part by an ID the Veneer declares.
It counts `Pack`, `Scaffold` and `Contract` hits for the strict ledger.

## `public static IReadOnlyList<TViolation> TAuditRun(`

Reads the contract IDs from the surface markup, then walks every driver source.
The pack scan reads raw lines, and the other two scans read the bound syntax.

## `private static void TAuditPackScan(string path, List<TViolation> violations)`

One hit per line holding a pack marker, named after the first marker found.
String literals are read too, since a pack URI is written as one.

## `private static void TAuditScaffoldScan(SyntaxNode root, List<TViolation> violations)`

One hit per type declaration whose base types include a scaffold type.
The walk stops at the first scaffold base, so a window counts once.

## `private static void TAuditContractScan(SyntaxNode root, IReadOnlySet<string> ids, List<TViolation> violations)`

Finds every call on the contract type and reads each string argument as an ID.
An ID that is not a constant, or that the markup never declares, is one hit.

## `private static IReadOnlySet<string> TAuditIdRead(IEnumerable<string> markupPaths)`

Every value of a contract ID attribute in the surface markup.
A file that does not parse is skipped here and reported by the reach walk.
