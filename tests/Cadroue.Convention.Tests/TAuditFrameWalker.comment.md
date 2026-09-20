# TAuditFrameWalker.cs

## `internal static class TAuditFrameWalker`

Reads every framework namespace and ambient member a pure ring names.
A namespace is read twice: from the using directive, and from the symbol a name binds to.
A package the project references beyond the runtime stays unbound, so its using directive is what is read.

## `private static readonly string TAuditFrameProject`

The project's own namespace prefix, never a framework namespace.

## `public static IReadOnlyList<TAuditHit> TAuditRun()`

Scans each tree of a pure ring on its own thread and returns every hit ordered by file and line.

## `private static List<TAuditHit> TAuditTreeScan(SemanticModel model, string relative, string ring)`

Walks one tree for the names bound to a type outside the sources.
A namespace outside the frame is a `frame` hit and a member under an ambient row an `ambient` hit.
One hit per line, kind and name, so a name used twice on a line counts once.

## `private static bool TAuditOutsideCheck(string space)`

True when the namespace is not in the frame, matched whole, so `System` never admits `System.IO`.

## `private static string? TAuditAmbientRead(ISymbol symbol, INamedTypeSymbol type, string space)`

The ambient row the symbol sits under, or null.
A member appends its own name, so `DateTime.UtcNow` is told apart from `DateTime`.
