# TAuditLookupWalker.cs
Hash: `6d59d41f701cec4c`

## `internal static class TAuditLookupWalker`

The same-lookup rule of Contesting, kept apart from origin following by role.
When every writer is one shell lookup, the property has one owner.
Contesting then compares the lookup's inputs, not the writers.

## `internal static (SyntaxNode?, List<SyntaxNode>)`

Null unless every writer is a call to one and the same shell method or constructor, judged by callee symbol.
A writer may reach the call through holders, as long as every holder writer does.
A holder's writers come from `TAuditOriginHolder.TAuditOriginRead`.
Otherwise it returns the first write with an engine input and every write with a plain input.
A write whose inputs are all keys is neither.

## `private static bool TAuditCallScan(ExpressionSyntax? value, List<ExpressionSyntax> calls, HashSet<ISymbol> visiting)`

Collects the shell calls a value comes from, through parentheses, casts, null-forgiving marks and holders.
A holder's writers that `TAuditOriginWalker` calls clear are skipped.
False when any path ends in something other than such a call.
A gate request is no lookup.

## `private static string TAuditInputResolve(ExpressionSyntax call)`

`engine`, `plain` or `key` for a call's arguments, its instance receiver and its initializer values.
A constant input is a key and is skipped.
Any engine input makes the call engine, else any plain input makes it plain.
