# TAuditTruthRelay.cs
Hash: `55ad73318110b1ce`

## `internal static partial class TAuditTruthWalker`

The relay half of the truth walker, covering which driver members stand in for logic and at which parameters.
Every set is keyed by symbol, so two members sharing a name never share a verdict.

## `private static HashSet<ISymbol> TAuditRelayNames`

The driver members whose body requests logic, so a call to one is a request.
A local function, a delegate field, a delegate property and an event count too.
Invoking one of those is then a request, so a request behind a delegate cannot hide.

## `private static HashSet<ISymbol>? TAuditPuppetNames`

The relay set as it stands before any event joins it.
An event is a signal, and the member that handles it is the one that requests.
So a control raising its own event, or forwarding a child's, is no puppeteer.
It stays null while the first pass grows, which keeps events out of the wiring.

## `internal static HashSet<ISymbol> TAuditReaderNames`

The driver members whose body reads or requests logic, so a value from one is an engine value.
A request is a read too, so a relay is read through as well.

## `private static Dictionary<ISymbol, HashSet<int>> TAuditHotNames`

For each relay, the positions of the parameters that reach logic inside it.
An argument at any other position is shown, not sent, and is no sink.

## `private static Dictionary<ISymbol, HashSet<int>> TAuditZeroingNames`

The hot positions Zeroing reads, a copy of `TAuditHotNames` grown further.
Constructors join, and a parameter stored into a member read inside a hot argument is hot too.
It is a separate dictionary, so every other kind keeps the narrower map unchanged.

## `private static HashSet<ISymbol> TAuditZeroingReads`

The driver fields and properties read inside an argument at a Zeroing hot position.
Only members of a shell type count, so a framework count or text never joins.
A literal zero compared with one of them is a hit.

## `private static void TAuditRelayRead(IReadOnlyList<TypeDeclarationSyntax> type)`

The requesting members of every driver, directly or through another relay, and the reading ones beside.
A delegate member becomes a relay when a driver assigns or subscribes a request, a relay or logic to it.
Only delegate-typed members of a shell type count, so a framework property never becomes a relay.
An interface event counts too, since a shell type raises it through its own implementation.
A delegate local becomes a relay when its initialiser or a later assignment is one.
A delegate parameter stored in a member or local passes on what its callers hand it.
Wiring, locals and seams are read over every tracked source, not only the walked drivers.
So a composition root that hands a gate to a driver constructor is traced too.
Every set and the hot positions grow to a fixed point, so a chain of wrappers is followed through.
The first fixed point leaves events out and is kept as `TAuditPuppetNames`.
Growth then resumes with events, so every other set reaches the same end.
The Zeroing map is built last, from the finished relay set and hot positions.

## `private static Dictionary<ISymbol, List<ExpressionSyntax>> TAuditSeamRead(IReadOnlyList<SyntaxNode> roots)`

Every argument handed to a delegate parameter, grouped by that parameter.
It reads every tracked source, so a caller in another type or project counts too.
It lets a stored parameter be judged by what its callers pass.

## `private static bool TAuditSeamCheck(ExpressionSyntax value, IReadOnlyDictionary<ISymbol, List<ExpressionSyntax>> seams, HashSet<ISymbol> seen)`

True when a value handed to a delegate member or local is a relay.
A bare delegate parameter is one when any caller hands it one, followed through every hop.
A parameter is visited once, so a cycle of callers ends.
Only a stored parameter is judged this way.
A parameter only invoked in place stays plain, since its caller's lambda already requests there.

## `private static IParameterSymbol? TAuditParameterRead(ArgumentSyntax argument)`

The parameter an argument binds to, by name when named, else by position.
A call, a construction, a `base` or `this` initialiser and a primary base call all bind.
Null when the call does not resolve to a method.

## `private static bool TAuditDelegateCheck(ExpressionSyntax value)`

True when a value handed to a delegate member requests.
Naming a relay or a logic method as a group counts too.
Naming a relay member or local without calling it counts as well.

## `private static bool TAuditHotRead(ISymbol symbol, SyntaxNode method, ParameterListSyntax list)`

Marks a parameter hot when the body passes it to logic or a hot relay position.
Passing a member of the parameter counts the same.
A method and a local function are read alike.
Returns true when a new position was found, so the caller loops again.

## `private static void TAuditZeroingRead(IReadOnlyList<TypeDeclarationSyntax> type)`

Builds the Zeroing map and its read members, starting from a copy of the current hot positions.
Methods, constructors, operators and local functions may hold hot parameters.
A parameter is hot when it appears in a hot argument, as in `TAuditHotRead`.
It is also hot when stored into a field or property that is read inside a hot argument.
A store into a field counts when a getter alias of it is read, so a backing field is followed.
The map and the read members grow together to a fixed point.
So a constructor whose stored id later reaches a gate gets a hot position.
`TAuditHotNames` is only copied, never written, so Replaying and the other kinds see no change.

## `private static HashSet<ISymbol> TAuditStoreRead(ExpressionSyntax target, IReadOnlyList<TypeDeclarationSyntax> nested, Dictionary<ISymbol, HashSet<ISymbol>> aliases)`

The stored member and its getter aliases, or an empty set when the target is no field or property.
A field's aliases come from `TAuditAliasRead` over its own type's parts only.
A property stands alone.
Each non-empty answer is cached by symbol, since the fixed point asks for the same store many times.

## `private static bool TAuditReadCheck(SyntaxNode member)`

True when the member's body reads a logic member or calls a reader.
A name inside `nameof` reads nothing and does not count.
