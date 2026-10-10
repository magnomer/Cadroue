# TAuditTruthSink.cs
Hash: `9c23cfa573bae31a`

## `internal static partial class TAuditTruthWalker`

The sink half of the truth walker, covering where a driver value may not go.

## `private static (string TViolationKind, string TViolationReason)? TAuditSinkRead(SyntaxNode reference)`

Walks up from the reference to the member and names the first sink met.
A reference that is only the receiver of a member access is not a sink.
Parentheses and a null-forgiving `!` between the two do not change that.
The owner of that member audits its own field.
The target of `?.` whose tail requests is gatekeeping, since its presence decides the request.
A hot argument is a replaying sink.
So is a value written into a record copy or the initialiser of a logic or relay construction.
The condition of an if, ternary or switch whose branch requests is a gatekeeping sink.
So is the condition of a while, do or for loop whose body requests.
So is a `when` clause on a switch arm or a catch that requests.
So is the left side of `&&`, `||` or `??` whose right side requests.
An if or ternary condition that only tests for null or for a type is not a gatekeeping sink.

## `private static bool TAuditGatekeepingCheck(IfStatementSyntax branch)`

True when a branch requests, or jumps out while the scope it leaves requests from its condition on.
A `return` or `throw` leaves the member.
A `continue` leaves its loop body, and a `break` leaves its loop or its switch section.
A request later in that loop or section is skipped by the jump, so it counts.
A request after the loop runs whatever the branch decides, so it does not count.
A request in a later case is not skipped by the `break`, so it does not count.
A request before the branch runs whatever the branch decides, so it is not gatekeeping.
A condition that only tests presence decides nothing the engine holds and is never gatekeeping.

## `private static bool TAuditPresenceCheck(ExpressionSyntax condition)`

True when the condition, past parentheses and negation, only tests for null or matches a type.
An `&&` or `||` chain counts only when every operand is such a test.

## `private static bool TAuditRequestCheck(SyntaxNode node)`

True when the node contains a logic call, a logic construction or a call to a relay.

## `private static bool TAuditHotCheck(ISymbol callee, ArgumentSyntax argument)`

True for any argument of a logic call, and for a relay argument standing at a hot position.
A named argument is matched to its parameter by name.

## `private static bool TAuditZeroingCheck(ArgumentSyntax argument, out ISymbol? callee)`

True when an argument of a call or construction stands at a Zeroing hot position.
Any argument of a logic call is hot, as in `TAuditHotCheck`.
Otherwise the position must sit in `TAuditZeroingNames`, never in `TAuditHotNames`.
The callee is the relay or logic symbol, else the plain method or constructor called.
So a driver constructor or helper outside the relay set can still be hot.
An indexer argument or a `this` or `base` initialiser is never hot.

## `private static void TAuditZeroingScan(SyntaxNode root, List<TViolation> violations)`

Reports every literal zero or default that a driver sends toward a gate as "none", "new" or "append".
From the literal it climbs through parentheses, casts, either arm of `?:` and the right side of `??`.
A literal that then stands at a Zeroing hot position is a hit named after the callee.
A constructor callee is named by its type, and the reason reads `passes 0 to new T`.
A literal compared with a hot-read member or a logic member is a hit named after that member.
The comparisons are `==`, `!=`, `<`, `>`, `<=`, `>=`, `is 0` and `is not 0`.
A count or length compared with zero reaches no gate, so it is no hit.
Each literal yields at most one hit, on the literal's own line.

## `private static bool TAuditZeroCheck(ExpressionSyntax node)`

True for the literal `0` or `0L`, the `default` literal and a `default(T)` expression.
A `null` literal is not one, since it names absence in the type itself.

## `private static ISymbol? TAuditComparedRead(ExpressionSyntax side)`

The field or property on the other side of a zero comparison, when that member counts.
Parentheses, casts, `?.` and a null-forgiving `!` are looked through to the member read.
It counts when read inside a Zeroing hot argument or when it is a logic member.
Otherwise null, so a framework count or a local compared with zero is no hit.

## `internal static ISymbol? TAuditCallRead(ExpressionSyntax call)`

The logic or relay symbol a call or construction invokes, else null.
Invoking a delegate member that is a relay returns that member.
Raising an event that implements a relay interface event returns that event.

## `private static ISymbol? TAuditDelegateRead(ExpressionSyntax call)`

The delegate member a call invokes, directly, through `Invoke` or through `?.Invoke`.

## `private static bool TAuditInterfaceCheck(ISymbol held)`

True when an event implements an interface event that a driver subscribed a relay to.
A subscription through `INotifyPropertyChanged` thus reaches every type that raises it.

## `internal static bool TAuditNameofCheck(SyntaxNode node)`

True for a `nameof` expression, which yields a name and reads no value.
A method a driver itself names `nameof` resolves to a symbol and is not one.
