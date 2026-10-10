# TAuditTruthWalker.cs
Hash: `a8a08e9c233a1c36`

## `internal static partial class TAuditTruthWalker`

Follows each driver field to where its value goes, by symbol.
A settable property and a positional record parameter are followed the same way.
Logic is what `TAuditBinderSide` says lies below the cut, Conduct's gates and the engine alike.
A request is a call into logic or into a driver relay of one.
A field holding a Conduct type is a handle, while a field holding an engine type is duplicating.
Partial classes are joined by their type symbol.
This part also holds the handle and alias rules that decide which fields the walker follows.
The field, local and sequence scans read nested types and field initializers with their outer class.
`TAuditTruthField` lists the audited fields of a class.
`TAuditTruthReference` finds and sorts the references to them.
`TAuditTruthContest` reports a field with both an engine and a shell writer.

## `private static readonly object TAuditGate = new();`

The walker keeps its state in statics, and the truth and strict facts run on parallel threads.
The identifier index in `TAuditTruthReference` is such state too.
Every entry point takes the gate so one walk finishes before another resets the state.

## `private static IReadOnlyList<SyntaxNode> TAuditRoots = [];`

The walked roots, so a write to a shared field from another class is still seen.

## `public static IReadOnlyList<TViolation> TAuditRun(IReadOnlyList<string> sourcePaths)`

Scans every driver file for tampering, spoonfeeding, misfiring and zeroing, then checks mismatching across the drivers.
It then names every control member in the relay set as puppeteering and checks every type.

## `internal static bool TAuditHandleCheck(ITypeSymbol type)`

True for a Conduct type, which a driver is meant to hold.
Also true for a listed handle, shown minimally and with any nullable mark dropped.

## `internal static HashSet<ISymbol> TAuditAliasRead(IFieldSymbol field, IReadOnlyList<TypeDeclarationSyntax> type)`

The field and every getter-only property of its class that reads it without requesting.
A getter reading such a property is followed too, to a fixed point.
Gatekeeping through one of them is gatekeeping on the field itself.
A parameter the field is passed to by `ref`, `out` or `in` is the field itself inside that method.
Such a parameter passed on again is followed too.

## `private static void TAuditPuppeteeringScan(List<TViolation> violations)`

Names every control member that stands in `TAuditPuppetNames`, one hit per member.
A member that reaches logic only by raising an event is no hit, since the handler requests.
A delegate field or property a driver fills still counts, since the control invokes it.
A method, property or field counts, while a local function or local stays with its member.
The hit lands on the member's first declaration in a walked driver source.
A control shows what it is handed, so a request inside one is driver work done in the wrong type.
The reader set is never consulted, so reading a Conduct record property is no hit.
Hits are sorted by path, line and name, so the order holds whatever the set's order.

## `internal static bool TAuditControlCheck(string name, string path)`

True when the type name starts with the control prefix of the driver ring holding the path.
`TAuditTruthSetting.TAuditControlPrefix` keys each prefix by its ring folder.

## `public static IReadOnlySet<ISymbol> TAuditReaderRead(IReadOnlyList<string> sourcePaths)`

The driver members that read logic or request, for the laundering scan.

## `private static Dictionary<INamedTypeSymbol, List<TypeDeclarationSyntax>> TAuditPartRead(IReadOnlyList<string> sourcePaths)`

Keeps the walked roots, groups the outermost types, builds the identifier index and reads the relays and senders.

## `private static void TAuditFieldCheck(TAuditTruthField field, IReadOnlyList<TypeDeclarationSyntax> type, List<TViolation> violations)`

A field holding an engine type is duplicating before any reference is read.
A field typed `object` is duplicating too, since an untyped slot hides what it holds.
A field whose name ends in `State` is duplicating too, since only the engine resolves a state.
A `??=` whose right side requests caches the answer and is duplicating.
Visits every reference to the field, outside its class only when the field is shared.
A use of a ref parameter the field was passed to counts as inside, wherever its method lies.
A write is sorted by `TAuditOriginWalker.TAuditWriterResolve`.
A `ref` write into a plain helper is sorted by the argument the helper assigns.
A fill without an argument empties the field and is sorted as a clear.
The sorted writes go to `TAuditTruthContest.TAuditContestCheck` in reference order.
A read is checked once per member scope.

## `private static void TAuditScopeCheck(TAuditTruthField field, MemberDeclarationSyntax scope, List<TViolation> violations)`

Checks the field and the locals it taints inside one member for a sink.
A hit is reported once per line and kind.
A name inside `nameof` is skipped.
A hit through a ref parameter names that parameter, as a hit through a local names the local.
