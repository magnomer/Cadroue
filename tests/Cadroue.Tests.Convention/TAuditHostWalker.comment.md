# TAuditHostWalker.cs

## `internal static class TAuditHostWalker`

Holds Host to construction and wiring, since Host holds no behaviour.
A statement passes as a declaration, an expression statement or a closing `return`.
An expression passes as a name, a literal, a member chain, a call or a construction.
An assignment, an `await` and a lambda pass too.
Anything else is a wiring hit on its line, with every statement nested in it.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)`

Scans the top-level statements and every member body of each host file.
A property accessor with a body is behaviour and counts whole.

## `private static void TAuditBlockScan(IReadOnlyList<StatementSyntax> statements, List<SyntaxNode> breaches)`

Checks each statement of one block.
A `return` passes only as the block's last statement, so an early return is a hit.

## `private static void TAuditExpressionScan(ExpressionSyntax expression, List<SyntaxNode> breaches)`

Checks one expression and everything it is built from.
A branch, an operator, a pattern, a cast or a conditional access is a hit.

## `private static void TAuditArgumentScan(IEnumerable<ArgumentSyntax> arguments, List<SyntaxNode> breaches)`

Checks each argument, and a `ref`, `in` or `out` argument is a hit.
