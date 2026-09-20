# TAuditStrictWalker.cs

## `internal static class TAuditStrictWalker`

Compiles the shell sources and applies the strict rules by symbol.
Logic is what `TAuditBinder` says is logic, and a class is a veneer by its folder or its base type.
Partial classes are joined by type symbol, and one veneer part makes the whole class a veneer.

## `private static readonly SyntaxKind[] TAuditFlowKinds =`

The syntax a veneer member may not contain: branches, loops, operators, patterns and local functions.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)`

Compiles every file, scans the walked ones for treatment, groups the classes, then scans storage and flow per class.
The veneer class names come back for the report.

## `private static bool TAuditVeneerCheck(INamedTypeSymbol symbol, IReadOnlyList<ClassDeclarationSyntax> type)`

True when the class derives from a control base or any part of it lives under a veneer root.

## `private static void TAuditStorageScan(ClassDeclarationSyntax part, bool veneer, List<TViolation> violations)`

Every mutable field of a veneer class, and every mutable static field anywhere.
A `readonly` or `const` field is a fixture, and a `null!` field is wired once and skipped.

## `private static void TAuditFlowScan(ClassDeclarationSyntax part, List<TViolation> violations)`

One hit per veneer member, naming each flow kind found in it.

## `private static void TAuditTreatScan(SyntaxNode root, List<TViolation> violations)`

One hit per line and reason where a logic value is an operand, query source, cast, `typeof`, attribute or condition.
Null checks, coalescing and the logical connectives are not treatment.

## `private static bool TAuditConditionCheck(ExpressionSyntax condition)`

True when the condition treats a logic value, and false for a null check or a bare verdict.

## `public static ExpressionSyntax TAuditCoreRead(ExpressionSyntax condition)`

The condition unwrapped from parentheses and negation.

## `private static bool TAuditVerdictCheck(ExpressionSyntax condition)`

True when the unwrapped condition is one logic call, member or name.
Asking the engine a question is not treating its data.

## `public static bool TAuditPatternCheck(PatternSyntax pattern)`

True for a null constant, its negation, an empty property pattern, or a declaration that only names the value.

## `public static bool TAuditNullCheck(ExpressionSyntax expression)`

True for the `null` or `default` literal.

## `public static bool TAuditDataCheck(SyntaxNode node)`

True when any name inside the node resolves to logic, whether by its symbol or by its type.

## `private static void TAuditGlyphScan(SyntaxNode root, List<TViolation> violations)`

One hit per identifier holding a character outside ASCII.
A look-alike glyph would let a name pass every prefix rule while reading as another.

## `private static bool TAuditWiredCheck(VariableDeclaratorSyntax variable)`

True for a field initialised to `null!`.

## `private static string TAuditMemberRead(MemberDeclarationSyntax member)`

A short label for a member, for the report.

## `private static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
