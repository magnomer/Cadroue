# TAuditTruthRelay.cs

## `internal static partial class TAuditTruthWalker`

The relay half of the truth walker: which driver members stand in for logic, and at which parameters.
Every set is keyed by symbol, so two members sharing a name never share a verdict.
It also holds the handle and alias rules that decide which fields the walker follows.

## `private static HashSet<ISymbol> TAuditRelayNames`

The driver members whose body requests logic, so a call to one is a request.
A local function, a delegate field, a delegate property and an event count too.
Invoking one of those is then a request, so a request behind a delegate cannot hide.

## `private static HashSet<ISymbol> TAuditReaderNames`

The driver members whose body reads or requests logic, so a value from one is an engine value.
A call to one of them is a request, so a private wrapper cannot launder a sink.

## `private static Dictionary<ISymbol, HashSet<int>> TAuditHotNames`

For each relay, the positions of the parameters that reach logic inside it.
An argument at any other position is shown, not sent, and is no sink.

## `private static void TAuditRelayRead(IReadOnlyList<TypeDeclarationSyntax> type)`

The requesting members of every driver, directly or through another relay, and the reading ones beside.
A delegate member becomes a relay when a driver assigns or subscribes a request, a relay or logic to it.
Only delegate-typed members declared in UI source count, so a framework property never becomes a relay.
Every set and the hot positions grow to a fixed point, so a chain of wrappers is followed through.

## `private static bool TAuditDelegateCheck(ExpressionSyntax value)`

True when a value handed to a delegate member requests.
Naming a relay or a logic method as a group counts too.

## `private static bool TAuditHotRead(ISymbol symbol, SyntaxNode method, ParameterListSyntax list)`

Marks a parameter hot when the body passes it, or its member, to logic or a hot relay position.
A method and a local function are read alike.
Returns true when a new position was found, so the caller loops again.

## `private static bool TAuditReadCheck(SyntaxNode member)`

True when the member's body reads a logic member or calls a reader.

## `private static bool TAuditHandleCheck(ITypeSymbol type)`

True for a Conduct type, which a driver is meant to hold.
Also true for a listed handle, shown minimally and with any nullable mark dropped.

## `private static HashSet<ISymbol> TAuditAliasRead(IFieldSymbol field, IReadOnlyList<TypeDeclarationSyntax> type)`

The field and every getter-only property of its class that reads it without requesting.
A getter reading such a property is followed too, to a fixed point.
A guard read through one of them is a guard on the field itself.
