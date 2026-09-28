# TAuditStrictWalker.cs

## `internal static class TAuditStrictWalker`

Applies the surface rules to the UI sources by symbol.
A type is a surface by its folder alone, since a driver uses its medium freely.
Partial types are joined by symbol, and one surface part makes the whole type a surface.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)`

Groups every type of the UI sources, then scans storage, calls, depth and members per surface type.
Every surface file is also scanned for glyphs, enums and delegates.
Outside the surfaces only a mutable static field is a hit.
The surface type names come back for the report.

## `private static bool TAuditVeneerCheck(SyntaxNode part)`

True when the node lives under a surface root.

## `private static void TAuditPlainScan(SyntaxNode root, List<TViolation> violations)`

Reads the enum and delegate declarations of a surface file, which no type scan reaches.
A name they carry from below the driver is a depth hit.
An enum value that is not a literal is a computation.

## `private static void TAuditStorageScan(TypeDeclarationSyntax part, bool veneer, List<TViolation> violations)`

Every field, event field, auto-property and primary constructor parameter of a surface type is storage.
A `readonly` or a wired `null!` field counts too, and a constant does not.
In a driver type, every mutable static field is a static hit.

## `private static void TAuditShellScan(TypeDeclarationSyntax part, List<TViolation> violations)`

One Shell hit per method, operator, property, event or indexer the surface part declares.
A constructor is the one member the shell keeps.

## `private static bool TAuditAutoCheck(PropertyDeclarationSyntax property)`

True for a property whose accessors have no body, which stores a value.

## `private static void TAuditCallScan(`

One hit per line of a surface member that is not a plain call.
Every statement nested in a breach is a breach of its own line, so a long branch counts in full.
The hit names the syntax kind that broke the rule.

## `private static IEnumerable<SyntaxNode> TAuditNestRead(SyntaxNode breach)`

The breach and every statement or switch arm inside it.

## `private static IEnumerable<SyntaxNode> TAuditBodyRead(MemberDeclarationSyntax member)`

Every body of a member: block, arrow, accessor bodies and a constructor initializer's arguments.
A field or property initializer is a body too, so a lambda there cannot hide its logic.

## `private static void TAuditBodyScan(SyntaxNode body, List<SyntaxNode> breaches)`

A block must hold only call statements, and an arrow, lambda body or initializer must be one call.

## `private static void TAuditStatementScan(StatementSyntax statement, List<SyntaxNode> breaches)`

A statement passes as a call statement or as a `return` of a call.
Any other statement is a breach, whether a branch, a loop, a declaration or a `try`.

## `private static void TAuditInvocationScan(ExpressionSyntax expression, List<SyntaxNode> breaches)`

The expression must be a call on a name or a plain member chain, with plain arguments.
A call to a query type's method is a breach, whether given a lambda or a method group.

## `private static void TAuditArgumentScan(ArgumentListSyntax arguments, List<SyntaxNode> breaches)`

Every argument must be a plain operand, and a `ref`, `in` or `out` argument is a breach.

## `private static void TAuditOperandScan(ExpressionSyntax operand, List<SyntaxNode> breaches)`

A plain operand is a name, `this`, `base`, a type, a literal, a member chain, a call or a lambda.
An operator, an assignment, a `new`, a cast, an `await` or a `?.` is a breach.

## `private static void TAuditEngineScan(SyntaxNode part, string owner, List<TViolation> violations)`

One depth hit per line of a surface declaration naming a type from below the driver.
Nested types are left to their own scan.

## `public static void TAuditGlyphScan(SyntaxNode root, List<TViolation> violations)`

One hit per identifier holding a character outside ASCII, under the glyph kind.
The surface walk and the driver walk each count it under their own audit.

## `public static ExpressionSyntax TAuditCoreRead(ExpressionSyntax condition)`

The condition unwrapped from parentheses and negation.

## `public static bool TAuditPatternCheck(PatternSyntax pattern)`

True for a null constant, its negation, an empty property pattern, or a declaration that only names the value.

## `public static bool TAuditNullCheck(ExpressionSyntax expression)`

True for the `null` or `default` literal.

## `public static bool TAuditDataCheck(SyntaxNode node)`

True when any name inside the node resolves below Conduct, by its symbol or by its type.
A Conduct value is the driver's to reshape, so it is not data here.

## `public static string TAuditMemberRead(MemberDeclarationSyntax member)`

A short label for a member, for the report.

## `public static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
