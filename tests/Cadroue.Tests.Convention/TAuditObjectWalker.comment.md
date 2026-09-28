# TAuditObjectWalker.cs

## `internal static class TAuditObjectWalker`

Merges every partial type of the bound sources and measures how tightly its parts are woven.
Binding goes through `TAuditBinder`, the compilation every bound audit and scripts/auditbinder.cs share.

## `public static IReadOnlyList<TAuditObjectRow> TAuditRun(IReadOnlyList<string> sourcePaths, string repoRoot)`

Takes the bound tree of every listed source, registers members in a first pass and links them in a second.
Returns one row per type with members, largest first.

## `private static void TAuditMemberScan(SemanticModel model, string repoRoot, Dictionary<INamedTypeSymbol, TAuditObjectType> types)`

Registers every type declaration in one tree as a part and every member it declares under the merged type.
A member already seen, as a partial method is, keeps its first part.

## `private static void TAuditLinkScan(SemanticModel model, Dictionary<INamedTypeSymbol, TAuditObjectType> types)`

Walks every simple name inside every member body and links the member to the same-type member it binds to.
A member is never linked to itself.

## `private static TAuditObjectMember? TAuditTargetResolve(SemanticModel model, SimpleNameSyntax name, TAuditObjectType type)`

The registered member one name binds to, or null.
An unresolved overload takes its first candidate.
An accessor resolves to its property or event.

## `private static IEnumerable<(ISymbol TAuditSymbol, bool TAuditState)> TAuditDeclaredRead(SemanticModel model, MemberDeclarationSyntax member)`

The symbols one member declaration declares, each flagged as state or not.
A field that is not a constant, a field-like event and an auto-property are state.
A nested type or delegate declares nothing here, since it is merged as its own type.

## `private static TAuditObjectRow TAuditRowCreate(TAuditObjectType type)`

Counts the cross references, finds the hubs, reads the weave with and without them and gives the verdict.
A monolith reaches both floors and either weaves without its hubs or crosses densely.

## `private static double TAuditWeaveRead(List<TAuditObjectMember> members, int partCount, HashSet<TAuditObjectMember> excluded)`

Joins members linked by use into components and returns the share of parts the widest one spans.
Excluded members join nothing, so hub state can be lifted out before the read.
