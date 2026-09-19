# TAuditTruthRelay.cs

## `internal static partial class TAuditTruthWalker`

The relay half of the truth walker: which shell members stand in for logic, and at which parameters.
Every set is keyed by symbol, so two members sharing a name never share a verdict.

## `private static HashSet<ISymbol> TAuditRelayNames`

The members of the shell whose body requests logic, so a call to one is a request.

## `private static HashSet<ISymbol> TAuditReaderNames`

The members of the shell whose body reads or requests logic, so a value from one is an engine value.
A call to one of them is a request, so a private wrapper cannot launder a sink.

## `private static Dictionary<ISymbol, HashSet<int>> TAuditHotNames`

For each relay, the positions of the parameters that reach logic inside it.
An argument at any other position is shown, not sent, and is no sink.

## `private static void TAuditRelayRead(IReadOnlyList<TypeDeclarationSyntax> type)`

The requesting members of the whole shell, directly or through another relay, and the reading ones beside.
Both sets and the hot positions grow to a fixed point, so a chain of wrappers is followed through.

## `private static bool TAuditHotRead(ISymbol symbol, MethodDeclarationSyntax method)`

Marks a parameter hot when the body passes it, or its member, to logic or a hot relay position.
Returns true when a new position was found, so the caller loops again.

## `private static bool TAuditReadCheck(MemberDeclarationSyntax member)`

True when the member's body reads a logic member or calls a reader.
