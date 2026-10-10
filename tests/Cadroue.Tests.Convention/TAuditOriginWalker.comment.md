# TAuditOriginWalker.cs
Hash: `1de4facff6e9f7ac`

## `internal static class TAuditOriginWalker`

Sorts a value written to a judged field by where it comes from, for Contesting.
It follows the value through Deportment holders to their writers.
Only a member of a logic-side type is an engine source, as before.
It is a class of its own, so the walker's big partial type gains no part.
`TAuditOriginHolder` reads what a holder is written with, and `TAuditOriginSite` finds the shell use sites.

## `private static readonly Dictionary<ISymbol, bool> TAuditOriginNames = new(SymbolEqualityComparer.Default);`

The verdict of each holder resolved from an empty path, cached for the compilation.
A verdict reached inside another resolve is not cached, since a cycle cut there depends on the path.

## `private static Compilation? TAuditOriginCompilation;`

The compilation the caches belong to.
An assay binds a new compilation, which empties the verdict cache and rebuilds the site index.

## `internal static string TAuditWriterResolve(ExpressionSyntax? value)`

`clear` for a blank value, else `engine` or `plain`.
`engine` when the value reads a logic member, calls logic or a reader, or names a logic-typed parameter or local.
It is also `engine` when the value names a holder whose origin is engine.
A compound assignment, increment or out argument has no value and is plain.
A name inside `nameof` reads no logic.
A call into the Capsule project is a shell source, and its arguments are not searched.
The Capsule reads and writes Deportment's own file, so its answer is the shell's whatever key goes in.
Writers outside the walked roots count, since every tracked shell source is indexed.

## `private static bool TAuditOriginCheck(ISymbol held, HashSet<ISymbol> visiting)`

True when the holder has a writer that is not a clear and every such writer is engine.
A holder met again on the path is a cycle and reads plain.
A writer that only copies a holder already on the path is skipped as neutral.
Such a copy, as in `held.X = fresh.X`, adds no origin of its own.
A holder written only by such copies has no writer left and stays plain.

## `internal static bool TAuditBlankCheck(ExpressionSyntax value)`

True for null, default, the empty string and `string.Empty`.
A blank write says nothing about the content, so it is a clear.
`false` and zero are left out, since writing them can be a real shell decision.

## `private static bool TAuditCapsuleCheck(SyntaxNode node)`

True for an invocation whose callee is declared under `TAuditCapsuleInclude`.
