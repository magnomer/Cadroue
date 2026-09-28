# TAuditTruthSink.cs

## `internal static partial class TAuditTruthWalker`

The sink half of the truth walker: where a driver value may not go.

## `private static (string TViolationKind, string TViolationReason)? TAuditSinkRead(SyntaxNode reference)`

Walks up from the reference to the member and names the first sink met.
A reference that is only the receiver of a member access is not a sink.
The owner of that member audits its own field.
The target of `?.` whose tail requests is a guard, since its presence decides the request.
An argument of a logic call, a record copy or a logic construction is an argument sink.
The condition of an if, ternary or switch whose branch requests is a guard sink.
So is the condition of a while, do or for loop whose body requests.
So is a `when` clause on a switch arm or a catch that requests.
So is the left side of `&&`, `||` or `??` whose right side requests.
A condition that only tests for null or for a type is not a guard sink.

## `private static bool TAuditGuardCheck(IfStatementSyntax branch)`

True when a branch requests, or when the branch jumps out and the member requests from its condition on.
A request before the branch runs whatever the branch decides, so it is not guarded.
A condition that only tests presence decides nothing the engine holds and is never a guard.

## `private static bool TAuditPresenceCheck(ExpressionSyntax condition)`

True when the condition, past parentheses and negation, only tests for null or matches a type.

## `private static bool TAuditRequestCheck(SyntaxNode node)`

True when the node contains a logic call, a logic construction or a call to a relay.

## `private static bool TAuditHotCheck(ISymbol callee, ArgumentSyntax argument)`

True for any argument of a logic call, and for a relay argument standing at a hot position.
A named argument is matched to its parameter by name.

## `private static ISymbol? TAuditCallRead(ExpressionSyntax call)`

The logic or relay symbol a call or construction invokes, else null.
Invoking a delegate member that is a relay returns that member.

## `private static ISymbol? TAuditDelegateRead(ExpressionSyntax call)`

The delegate member a call invokes, directly, through `Invoke` or through `?.Invoke`.
