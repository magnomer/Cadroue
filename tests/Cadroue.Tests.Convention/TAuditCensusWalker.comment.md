# TAuditCensusWalker.cs
Hash: `301bfbc58a822e20`

## `internal static class TAuditCensusWalker`

Reads what each ring holds.
That is its declared types and its source file count.
A ring is any project the border names, read from the bound trees.

## `public static IReadOnlyList<TAuditHit> TAuditDeclaredRead()`

Every class, struct, record, interface and enum declared inside a ring.
Each is a `declared` hit naming the ring and the type.

## `public static IReadOnlyDictionary<string, int> TAuditFileRead()`

The source file count of every ring.
