# TAuditTruthMutation.cs

## `internal static partial class TAuditTruthWalker`

The mutation half of the truth walker: what a shell line may not do to logic without a request.

## `private static void TAuditMutationScan(SyntaxNode root, List<TViolation> violations)`

Every plain assignment to a logic-named member outside an object initialiser.
An event subscription is a compound assignment and is not reported.
A tuple assignment over indexed slots is a swap, and a swap reorders a collection.
A `with` copy that sets a logic member rewrites logic the engine handed over.
A slot assignment on a store of rows overwrites what the engine ordered.
Also every order verb on a store of rows, since the engine alone owns an order.

## `private static bool TAuditStoreCheck(ExpressionSyntax rows)`

True when the collection's type holds logic, shell rows or untyped objects.
An expression the compiler cannot type is taken as a store, since the walk cannot clear it.

