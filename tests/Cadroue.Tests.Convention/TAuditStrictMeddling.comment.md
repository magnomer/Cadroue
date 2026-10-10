# TAuditStrictMeddling.cs
Hash: `dd697aec78b53c98`

## `internal static class TAuditStrictMeddling`

Judges each member of a surface type against the one rule that only a call may stand.
`TAuditStrictWalker` hands it every member of a surface part.
It walks bodies down to their operands and collects every breach.

## `public static void TAuditMeddlingScan(string owner, IEnumerable<MemberDeclarationSyntax> members, List<TViolation> violations)`

One hit per line of a surface member that is not a plain call.
Every statement nested in a breach is a breach of its own line, so a long branch counts in full.
The hit names the syntax kind that broke the rule.

## `private static IEnumerable<SyntaxNode> TAuditNestRead(SyntaxNode breach)`

The breach and every statement or switch arm inside it.

## `private static IEnumerable<SyntaxNode> TAuditBodyRead(MemberDeclarationSyntax member)`

Every body of a member, meaning block, arrow, accessor bodies and a constructor initializer's arguments.
A field or property initializer is a body too, so a lambda there cannot hide its logic.

## `private static void TAuditBodyScan(SyntaxNode body, List<SyntaxNode> breaches)`

A block must hold only call statements, and an arrow, lambda body or initializer must be one call.

## `private static void TAuditStatementScan(StatementSyntax statement, List<SyntaxNode> breaches)`

A statement passes as a call statement or as a `return` of a call.
Any other statement is a breach, whether a branch, a loop, a declaration or a `try`.

## `private static void TAuditInvocationScan(ExpressionSyntax expression, List<SyntaxNode> breaches)`

The expression must be a call on a name or a plain member chain, with plain arguments.
A call to a method of a query type is a breach.

## `private static void TAuditArgumentScan(ArgumentListSyntax arguments, List<SyntaxNode> breaches)`

Every argument must be a plain operand, and a `ref`, `in` or `out` argument is a breach.

## `private static void TAuditOperandScan(ExpressionSyntax operand, List<SyntaxNode> breaches)`

A plain operand is a name, `this`, `base`, a predefined type, a literal or a member chain.
A call or a lambda is plain too.
A lambda is judged by its own body.
An operator, an assignment, a `new`, a cast, an `await` or a `?.` is a breach.
