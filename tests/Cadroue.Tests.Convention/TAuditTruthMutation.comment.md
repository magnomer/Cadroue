# TAuditTruthMutation.cs

## `internal static partial class TAuditTruthWalker`

The mutation half of the truth walker: what a driver line may not do to logic without a request.
It also holds the feed rule and the parity rule, which read every driver file at once.

## `private static void TAuditMutationScan(SyntaxNode root, List<TViolation> violations)`

Every plain assignment to a logic-named member outside an object initialiser.
An event subscription is a compound assignment and is not reported.
A tuple assignment over indexed slots is a swap, and a swap reorders a collection.
A `with` copy that sets a logic member rewrites logic the engine handed over.
A slot assignment on a store of rows overwrites what the engine ordered.
A slot assignment on a local the member built empty fills it and is not reported.
Also every order verb on a store of rows, since the engine alone owns an order.
The feed scan runs first on the same file.

## `private static void TAuditFeedScan(SyntaxNode root, List<TViolation> violations)`

Every value of a type from below the cut that a driver hands a control or a surface type.
An assignment, an object initializer, a call argument and a construction argument are each a handover.
A surface binding reads the members of what it is fed.
So typed bindings are held here rather than in markup.
A lambda is wiring rather than data and is left alone.

## `private static bool TAuditSurfaceRead(ExpressionSyntax receiver)`

True when the receiver, or any object along its member chain, is a control or a surface type.

## `private static void TAuditParityScan(List<TViolation> violations)`

Every Conduct member one driver reaches and another never does, once per file and member.
Every Conduct interface that a driver declares no implementation of, once per driver.
Both drivers stand on one Conduct, so an asymmetry is medium work or a leak, and the ledger holds it.

## `private static bool TAuditFreshCheck(ExpressionSyntax rows)`

True when the collection is a local declared empty, by an empty collection expression or a bare construction.
Such a local holds nothing the engine handed over.

## `private static bool TAuditStoreCheck(ExpressionSyntax rows)`

True when the collection's type holds logic, shell rows or untyped objects.
An expression the compiler cannot type is taken as a store, since the walk cannot clear it.

