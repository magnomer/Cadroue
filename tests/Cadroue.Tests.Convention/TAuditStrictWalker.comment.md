# TAuditStrictWalker.cs
Hash: `723eb9e5bd2465ff`

## `internal static class TAuditStrictWalker`

Applies the surface rules to the UI sources by symbol.
A type is a surface by its folder alone, since a driver uses its medium freely.
Partial types are joined by symbol, and one surface part makes the whole type a surface.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)`

Groups every type of the UI sources, then scans hoarding, meddling, prying and freelancing per surface type.
The meddling scan of each member lives in `TAuditStrictMeddling`.
Every surface file is also scanned for homoglyphs, enums and delegates.
Outside the surfaces only a mutable static field is a hit.
The surface type names come back for the report.

## `private static bool TAuditVeneerCheck(SyntaxNode part)`

True when the node lives under a surface root.

## `private static void TAuditPlainScan(SyntaxNode root, List<TViolation> violations)`

Reads the enum and delegate declarations of a surface file, which no type scan reaches.
A name they carry from below the driver is a prying hit.
An enum value that is not a literal is a computation.

## `private static void TAuditHoardingScan(TypeDeclarationSyntax part, bool veneer, List<TViolation> violations)`

Every field, event field, auto-property and primary constructor parameter of a surface type is hoarding.
A `readonly` or a wired `null!` field counts too, and a constant does not.
In a driver type, every mutable static field is a sharing hit.

## `private static void TAuditFreelancingScan(TypeDeclarationSyntax part, List<TViolation> violations)`

One freelancing hit per method, operator, property, event or indexer the surface part declares.
A constructor is the one member the shell keeps.

## `private static bool TAuditAutoCheck(PropertyDeclarationSyntax property)`

True for a property whose accessors have no body, which stores a value.

## `private static void TAuditEngineScan(SyntaxNode part, string owner, List<TViolation> violations)`

One prying hit per line of a surface declaration naming a type from below the driver.
Nested types are left to their own scan.

## `public static void TAuditHomoglyphScan(SyntaxNode root, List<TViolation> violations)`

One hit per identifier holding a character outside ASCII, under the homoglyph kind.
The surface walk and the driver walk each count it under their own audit.

## `public static string TAuditMemberRead(MemberDeclarationSyntax member)`

A short label for a member, for the report.

## `public static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
