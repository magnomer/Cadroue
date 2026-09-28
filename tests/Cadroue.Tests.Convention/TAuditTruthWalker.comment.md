# TAuditTruthWalker.cs

## `internal static partial class TAuditTruthWalker`

Follows each driver field to where its value goes, by symbol.
A settable property and a positional record parameter are followed the same way.
Logic is what `TAuditBinder` says lies below the cut, Conduct's gates and the engine alike.
A request is a call into logic or into a driver relay of one.
A field holding a Conduct type is a handle, while a field holding an engine type is a mirror.
Partial classes are joined by their type symbol.
The field, local and sequence scans read nested types and field initializers with their outer class.

## `private static readonly object TAuditGate = new();`

The walker keeps its state in statics, and the truth and strict facts run on parallel threads.
Every entry point takes the gate so one walk finishes before another resets the state.

## `private static IReadOnlyList<SyntaxNode> TAuditRoots = [];`

The walked roots, so a write to a shared field from another class is still seen.

## `private static Dictionary<ISymbol, List<IdentifierNameSyntax>> TAuditIndex`

Every identifier of the walked roots, grouped by the symbol it resolves to.
A field's references are looked up here rather than found by scanning every file again.

## `private sealed record TAuditTruthField(`

One audited field: the symbols that mean it, its label, its type and where it is declared.
A positional parameter carries both its parameter symbol and the property it generates.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)`

Scans every driver file for mutation, feed and shape, checks parity across the drivers, then checks every type.

## `public static IReadOnlySet<ISymbol> TAuditReaderRead(IReadOnlyList<string> sourcePaths)`

The driver members that read logic or request, for the taint scan.

## `private static Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> TAuditPartRead(`

Compiles, keeps the walked roots, builds the identifier index, groups the outermost types and reads the relays.

## `private static IEnumerable<TAuditTruthField> TAuditFieldRead(IReadOnlyList<TypeDeclarationSyntax> type)`

The mutable fields, the settable properties and the positional record parameters of a class.
A `readonly` or `const` field is a fixture only while nothing writes into it and it holds no engine type.
A `static` field is shared and its writes are read in every file.
A field initialised to `null!` is audited like any other, since its type says what it holds.
A field typed as a handle grips a gate rather than holding a value, and is skipped.
A getter property that reads the field is followed as the field, so a guard cannot hide behind it.

## `private static bool TAuditFillCheck(HashSet<ISymbol> symbols, IReadOnlyList<TypeDeclarationSyntax> type)`

True when any part of the class writes into the field, by assignment, slot or fill verb.

## `private static IEnumerable<IdentifierNameSyntax> TAuditUseRead(IEnumerable<ISymbol> symbols)`

Every identifier resolving to one of the symbols, from the index.

## `private static bool TAuditInsideCheck(SyntaxNode node, IReadOnlyList<TypeDeclarationSyntax> type)`

True when the node sits inside one of the class parts.

## `private static bool TAuditFieldCheck(IdentifierNameSyntax identifier, HashSet<ISymbol> symbols)`

True when the identifier resolves to one of the symbols.

## `private static void TAuditFieldCheck(`

A field whose type is from below Conduct is a mirror before any reference is read.
A field typed `object` is a mirror too, since an untyped slot hides what it holds.
A field whose name ends in `State` is a mirror too, since only the engine resolves a state.
A `??=` whose right side requests caches the answer and is a mirror.
Visits every reference to the field, outside its class only when the field is shared.
A write is sorted by its writer.
A field with an engine writer and a shell writer is a fork.
The hit names the engine write by file when that write lies in another file.
A read is checked once per member scope.

## `private static void TAuditScopeCheck(`

Checks the field and the locals it taints inside one member for a sink.
A hit is reported once per line and kind.

## `private static HashSet<ISymbol> TAuditTaintRead(TAuditTruthField field, MemberDeclarationSyntax scope)`

Locals whose initialiser, assignment or pattern designation reads the field.
One hop only, so a local built from a tainted local is not followed.

## `private static void TAuditSymbolAdd(SyntaxNode node, HashSet<ISymbol> symbols)`

Adds the symbol a declaration or name resolves to, when it resolves.

## `private static string TAuditWriterResolve(ExpressionSyntax? value)`

`clear` for null or default, else `engine` or `plain`.
`engine` when the value reads a logic member, calls logic or a reader, or names a logic-typed parameter or local.
A compound assignment, increment or out argument has no value and is plain.

## `private static SyntaxNode TAuditReferenceRead(IdentifierNameSyntax identifier)`

The reference node: the access when the identifier is a member of `this` or a named owner, else itself.

## `private static bool TAuditWriteCheck(SyntaxNode reference, out ExpressionSyntax? value)`

True when the reference is assigned, incremented or passed by out or ref.
Also true when a slot of it is assigned or a fill verb is called on it.
The value is the right side of a plain assignment, or the last argument of a fill.
A fill without an argument empties the field and is sorted as a clear, not a writer.

## `private static bool TAuditNameCheck(SyntaxNode node, HashSet<ISymbol> symbols)`

True when the node contains an identifier resolving to one of the symbols.

## `private static int TAuditLineRead(SyntaxNode node)`

The one-based line of the node.
