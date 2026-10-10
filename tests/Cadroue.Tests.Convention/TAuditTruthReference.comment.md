# TAuditTruthReference.cs
Hash: `3c96dcfa5aa99002`

## `internal static class TAuditTruthReference`

Finds the references to a symbol in the walked roots and says what each reference does.
The truth walker's parts, its field, contest and origin helpers all share these answers.
The index is static state, so it is only rebuilt under `TAuditTruthWalker`'s gate.

## `private static Dictionary<ISymbol, List<IdentifierNameSyntax>> TAuditIndex`

Every identifier of the walked roots, grouped by the symbol it resolves to.
A field's references are looked up here rather than found by scanning every file again.
A name inside `nameof` reads nothing, so it is left out of the index.

## `internal static void TAuditIndexResolve(IReadOnlyList<SyntaxNode> roots)`

Rebuilds the index from the walked roots, dropping every entry of the last walk.

## `internal static IEnumerable<IdentifierNameSyntax> TAuditUseRead(IEnumerable<ISymbol> symbols)`

Every identifier resolving to one of the symbols, from the index.

## `internal static bool TAuditInsideCheck(SyntaxNode node, IReadOnlyList<TypeDeclarationSyntax> type)`

True when the node sits inside one of the class parts.

## `internal static bool TAuditFieldCheck(IdentifierNameSyntax identifier, HashSet<ISymbol> symbols)`

True when the identifier resolves to one of the symbols.

## `internal static HashSet<ISymbol> TAuditTaintRead(TAuditTruthField field, MemberDeclarationSyntax scope)`

Locals whose initialiser, assignment or pattern designation reads the field.
One hop only, so a local built from a tainted local is not followed.

## `internal static void TAuditSymbolAdd(SyntaxNode node, HashSet<ISymbol> symbols)`

Adds the symbol a declaration or name resolves to, when it resolves.

## `internal static SyntaxNode TAuditReferenceRead(IdentifierNameSyntax identifier)`

The reference node is the access when the identifier is a member of `this` or a named owner, else itself.

## `internal static bool TAuditWriteCheck(SyntaxNode reference, out ExpressionSyntax? value)`

True when the reference is assigned, incremented or passed by out or ref.
Also true when a slot of it is assigned or a fill verb is called on it.
The value is the right side of a plain assignment, or the last argument of a fill.
A fill without an argument empties the field and is sorted as a clear, not a writer.

## `internal static bool TAuditNameCheck(SyntaxNode node, HashSet<ISymbol> symbols)`

True when the node contains an identifier resolving to one of the symbols.
A name inside `nameof` does not count.

## `internal static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
