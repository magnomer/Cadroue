# TAuditObjectType.cs

## `internal sealed class TAuditObjectType(INamedTypeSymbol symbol)`

A type under merge: its symbol, the parts found so far and every member keyed by symbol.
The member map is keyed with the symbol comparer, so a reference bound in any part finds its target.
