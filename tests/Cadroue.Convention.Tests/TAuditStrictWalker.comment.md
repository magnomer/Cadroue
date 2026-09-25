# TAuditStrictWalker.cs

## `internal static class TAuditStrictWalker`

Compiles the UI sources and applies the veneer rules by symbol.
A type is a veneer by its folder alone, since the deportment uses WPF freely.
Partial types are joined by symbol, and one veneer part makes the whole type a veneer.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths, out List<string> veneers)`

Groups every type of the UI sources, then scans storage, calls and engine reach per veneer type.
Outside the veneer only a mutable static field is a hit.
The veneer type names come back for the report.

## `private static bool TAuditVeneerCheck(IReadOnlyList<TypeDeclarationSyntax> type)`

True when any part of the type lives under a veneer root.

## `private static void TAuditStorageScan(TypeDeclarationSyntax part, bool veneer, List<TViolation> violations)`

Every field, event field, auto-property and primary constructor parameter of a veneer type.
A veneer holds nothing, so `readonly`, `const` and a wired `null!` field count too.
Outside the veneer, every mutable static field.

## `private static bool TAuditAutoCheck(PropertyDeclarationSyntax property)`

True for a property whose accessors have no body, which stores a value.

## `private static void TAuditCallScan(TypeDeclarationSyntax part, List<TViolation> violations)`

One hit per line of a veneer member that is not a plain call.
The hit names the first syntax kind that broke the rule.

## `private static IEnumerable<SyntaxNode> TAuditBodyRead(MemberDeclarationSyntax member)`

Every body of a member: block, arrow, accessor bodies and a constructor initializer's arguments.

## `private static void TAuditBodyScan(SyntaxNode body, List<SyntaxNode> breaches)`

A block must hold only call statements, and an arrow or a lambda body must be one call.

## `private static void TAuditStatementScan(StatementSyntax statement, List<SyntaxNode> breaches)`

A statement passes as a call statement or as a `return` of a call.
Any other statement is a breach, whether a branch, a loop, a declaration or a `try`.

## `private static void TAuditInvocationScan(ExpressionSyntax expression, List<SyntaxNode> breaches)`

The expression must be a call on a name or a plain member chain, with plain arguments.

## `private static void TAuditArgumentScan(ArgumentListSyntax arguments, List<SyntaxNode> breaches)`

Every argument must be a plain operand, and a `ref`, `in` or `out` argument is a breach.

## `private static void TAuditOperandScan(ExpressionSyntax operand, List<SyntaxNode> breaches)`

A plain operand is a name, `this`, `base`, a type, a literal, a member chain, a call or a lambda.
An operator, an assignment, a `new`, a cast, an `await` or a `?.` is a breach.

## `private static void TAuditEngineScan(TypeDeclarationSyntax part, List<TViolation> violations)`

One hit per line of a veneer type that names an engine symbol or a value of an engine type.
Nested types are left to their own scan.

## `public static ExpressionSyntax TAuditCoreRead(ExpressionSyntax condition)`

The condition unwrapped from parentheses and negation.

## `public static bool TAuditPatternCheck(PatternSyntax pattern)`

True for a null constant, its negation, an empty property pattern, or a declaration that only names the value.

## `public static bool TAuditNullCheck(ExpressionSyntax expression)`

True for the `null` or `default` literal.

## `public static bool TAuditDataCheck(SyntaxNode node)`

True when any name inside the node resolves to logic, whether by its symbol or by its type.

## `private static string TAuditMemberRead(MemberDeclarationSyntax member)`

A short label for a member, for the report.

## `private static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
