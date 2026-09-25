# TAuditTruthSink.cs

## `internal static partial class TAuditTruthWalker`

The sink half of the truth walker: where a field value may not go.

## `private static (string TSinkKind, string TSinkReason)? TAuditSinkRead(SyntaxNode reference)`

Walks up from the reference to the member and names the first sink met.
A reference that is only the receiver of a member access is not a sink.
The owner of that member audits its own field.
An argument of a logic call, a record copy or a logic construction is an argument sink.
The condition of an if, ternary or switch whose branch requests is a guard sink.
A condition that only tests for null or for a type is not a guard sink.

## `private static bool TAuditGuardCheck(IfStatementSyntax branch)`

True when a branch requests, or when the branch jumps out and the member requests from its condition on.
A request before the branch runs whatever the branch decides, so it is not guarded.
A condition that only tests presence decides nothing the engine holds and is never a guard.

## `private static bool TAuditPresenceCheck(ExpressionSyntax condition)`

True when the condition, past parentheses and negation, only tests for null or matches a type.

## `private static bool TAuditRequestCheck(SyntaxNode node)`

True when the node contains a logic call or a logic construction.

## `private static bool TAuditHotCheck(ISymbol callee, ArgumentSyntax argument)`

True for any argument of a logic call, and for a relay argument standing at a hot position.
A named argument is never matched by position and is left alone.

## `private static ISymbol? TAuditCallRead(ExpressionSyntax call)`

The logic or relay symbol a call or construction invokes, else null.

