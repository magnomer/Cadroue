# TAuditTruthMutation.cs

## `internal static partial class TAuditTruthWalker`

The mutation half of the truth walker: what a shell line may not do to logic without a request.

## `private static void TAuditMutationScan(SyntaxNode root, List<TViolation> violations)`

Every plain assignment to a logic-named member outside an object initialiser.
An event subscription is a compound assignment and is not reported.
A tuple assignment over indexed slots is a swap, and a swap reorders a collection.
A `with` copy that sets a logic member rewrites logic the engine handed over.
A slot assignment on a store of rows overwrites what the engine ordered.
A slot assignment on a local the member built empty fills it and is not reported.
Also every order verb on a store of rows, since the engine alone owns an order.

## `private static bool TAuditFreshCheck(ExpressionSyntax rows)`

True when the collection is a local declared empty, by an empty collection expression or a bare construction.
Such a local holds nothing the engine handed over.

## `private static bool TAuditStoreCheck(ExpressionSyntax rows)`

True when the collection's type holds logic, shell rows or untyped objects.
An expression the compiler cannot type is taken as a store, since the walk cannot clear it.

