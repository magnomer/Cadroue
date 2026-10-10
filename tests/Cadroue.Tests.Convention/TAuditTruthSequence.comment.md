# TAuditTruthSequence.cs
Hash: `a5a77f14c0b84d98`

## `internal static partial class TAuditTruthWalker`

The sequence half of the truth walker.
One driver member may not dictate a run of requests.
One user action is one gate, and the order of several is a gate's decision.
Line numbers come from `TAuditTruthReference`.

## `private static HashSet<ISymbol> TAuditSendNames`

The members that send a request directly, read before the walk.

## `private static void TAuditSendResolve(IReadOnlyList<TypeDeclarationSyntax> parts)`

Every method or property that reaches a send root, calls a Conduct method or builds a request record.
One hop only, so a member that merely shows or fills is not a sender.

## `private static List<SyntaxNode> TAuditSendRead(SyntaxNode scope, bool direct)`

The send sites inside a scope in source order.
A root call, a Conduct call, or a request record built outside a send.
A call to a sender counts too when not direct.
A site nested inside another send counts once.

## `private static bool TAuditSendCheck(InvocationExpressionSyntax call, bool direct)`

True when the callee is a send root, a Conduct method, or a sender when the walk follows one hop.

## `private static bool TAuditGateCheck(ISymbol callee)`

True for an instance method declared on a Conduct type, which is a gate call.

## `private static void TAuditSequenceScan(IReadOnlyList<TypeDeclarationSyntax> type, List<TViolation> violations)`

Every member whose sends are not all exclusive is reported once.
It is reported on the first send that clashes with an earlier one.

## `private static bool TAuditExclusiveCheck(SyntaxNode earlier, SyntaxNode later)`

Two sends are exclusive when their nearest shared ancestor is a branch and they sit in different arms.
An earlier send inside an `if` that jumps out is exclusive with what follows it.

## `private static SyntaxNode TAuditArmRead(SyntaxNode node, SyntaxNode shared)`

The child of the shared ancestor that holds the node.

## `private static bool TAuditJumpCheck(IfStatementSyntax branch)`

True for an `if` without `else` whose body always leaves by `return`, `throw`, `break` or `continue`.
Only then can a send inside the body and a send after the `if` never both run.
A jump counts only when plain blocks alone lie between it and the `if`.
A jump under a nested `if`, loop, switch, `try`, `using`, lambda or local function may not run.
So a `break` or `continue` that counts always targets a loop or switch enclosing the `if`.
A `throw` expression such as `x ?? throw` is not a jump statement and does not count.
