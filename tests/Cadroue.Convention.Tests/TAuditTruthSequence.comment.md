# TAuditTruthSequence.cs

## `internal static partial class TAuditTruthWalker`

The sequence half of the truth walker: one shell member may not dictate a run of requests.
A gesture is one request, and the order of several is an engine decision.

## `private static HashSet<ISymbol> TAuditSendNames`

The members that send a request directly, read before the walk.

## `private static void TAuditSendResolve(IReadOnlyList<TypeDeclarationSyntax> parts)`

Every method or property that reaches a send root or builds a request record.
One hop only, so a member that merely shows or fills is not a sender.

## `private static List<SyntaxNode> TAuditSendRead(SyntaxNode scope, bool direct)`

The send sites inside a scope in source order.
A root call, a request record built outside a send, or a call to a sender when not direct.
A site nested inside another send counts once.

## `private static bool TAuditSendCheck(InvocationExpressionSyntax call, bool direct)`

True when the callee is a send root, or a sender when the walk follows one hop.

## `private static void TAuditSequenceScan(IReadOnlyList<TypeDeclarationSyntax> type, List<TViolation> violations)`

Every member whose sends are not all exclusive is reported once, on the second send.

## `private static bool TAuditExclusiveCheck(SyntaxNode earlier, SyntaxNode later)`

Two sends are exclusive when their nearest shared ancestor is a branch and they sit in different arms.
An earlier send inside an `if` that jumps out is exclusive with what follows it.

## `private static SyntaxNode TAuditArmRead(SyntaxNode node, SyntaxNode shared)`

The child of the shared ancestor that holds the node.

## `private static bool TAuditJumpCheck(IfStatementSyntax branch)`

True for an `if` without `else` whose body returns, throws, continues or breaks.
