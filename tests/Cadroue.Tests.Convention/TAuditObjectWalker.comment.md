# TAuditObjectWalker.cs
Hash: `6f0da4a7cb6ec6f4`

## `internal static class TAuditObjectWalker`

Merges every partial type of the bound sources and measures how tightly its parts are glued.
It also measures how far each type couples to other codebase types, and how much state it rebinds.
Binding goes through `TAuditBinder`, the compilation every bound audit shares.

## `public static IReadOnlyList<TAuditObjectRow> TAuditRun(IReadOnlyList<string> sourcePaths)`

Takes the bound tree of every listed source, registers members in a first pass and links them in a second.
A third pass collects the type uses of every codebase type, member-less ones included.
Incoming is counted only after every use is known, since any type may name any other.
Returns one row per type with members, longest first, then by name.
A type without members gets no row, yet its uses still count toward the Incoming of others.

## `private static void TAuditMemberScan(SemanticModel model, Dictionary<INamedTypeSymbol, TAuditObjectType> types)`

Registers every type declaration in one tree as a part and every member it declares under the merged type.
A partial method or property registers once, on the part that implements it.

## `private static void TAuditLinkScan(SemanticModel model, Dictionary<INamedTypeSymbol, TAuditObjectType> types)`

Walks every simple name inside every member declaration and links the member to the same-type member it binds to.
A member is never linked to itself.

## `private static void TAuditUseScan(SemanticModel model, HashSet<INamedTypeSymbol> codebase, Dictionary<INamedTypeSymbol, HashSet<INamedTypeSymbol>> uses)`

Records, for every type declared in one tree, the codebase types its names bind to.
A codebase type is any class, struct, interface, record, enum or delegate declared in a walked file.
Every name counts, in a signature, a member type or a body alike.
A name that binds only to candidates takes its first candidate.
Names inside a nested type or delegate belong to that nested type, not to the outer one.
A type never uses itself, a type nested in it, or a type it is nested in.
Partial parts add to one set, so a use between parts of one type drops out.

## `private static IEnumerable<INamedTypeSymbol> TAuditUseRead(ISymbol? symbol)`

The types one bound symbol stands for.
An alias gives its target, an array its element and a pointer its pointed-at type.
A named type gives its original definition and every type in its arguments.
An anonymous type gives nothing.
A member gives only the original definition of its containing type.
Namespaces, locals, parameters, type parameters and other symbols give nothing.

## `private static bool TAuditNestCheck(INamedTypeSymbol first, INamedTypeSymbol second)`

True when either type is nested at any depth inside the other.

## `private static TAuditObjectMember? TAuditTargetResolve(SemanticModel model, SimpleNameSyntax name, TAuditObjectType type)`

The registered member one name binds to, or null.
An unresolved overload takes its first candidate.
An accessor resolves to its property or event.

## `private static IEnumerable<(ISymbol TAuditSymbol, bool TAuditState, bool TAuditMutable)> TAuditDeclaredRead(SemanticModel model, MemberDeclarationSyntax member)`

The symbols one member declaration declares, each flagged as state and as mutable or not.
State marks the slots a hub is looked for in.
A field that is not a constant, a field-like event and a property with no accessor body are state.
Mutable is a field neither constant nor readonly, or a property with a backing field and a non-init setter.
A static field counts like an instance field.
A readonly field never rebinds, so it is not mutable whatever its type holds.
A get-only or init-only property is set only during construction, so it is not mutable either.
An event only notifies, so it is not mutable.
A captured primary-constructor parameter is neither state nor mutable, though the compiler stores it.
This is a known limit, since the parameter declares no member of the type.
The defining part of a partial method or property declares nothing when an implementing part exists.
The implementing part stands for it under the defining symbol, which references bind to.
A partial property is state and mutable as its implementing part is.
A nested type or delegate declares nothing here, since it is merged as its own type.

## `private static TAuditObjectRow TAuditRowCreate(TAuditObjectType type, int outgoing, int incoming)`

Counts the crossings, finds the hubs and reads Glued, then Fused without them.
Outgoing and incoming arrive from the use pass, since they need other types.
The flags are read from the finished numbers, and the verdict is the first flag.
With no flag the verdict is Colony for several parts and Hermit for one.
A hub leaves the verdict alone, since it is a finding on a slot outside the ladder.

## `private static List<string> TAuditFlagRead(TAuditObjectRow row)`

Every flag the merged type hits, in ladder order from Hydra down to Serpent.
Each limit comes from its verdict dictionary in `TAuditObjectSetting` and is reached at its value or above.
A Hydra reaches the parts and lines limits, and either the fused or the density limit.
A Kraken hits the size axis and the coupling axis together.
The size axis is a Serpent or Centipede condition.
The coupling axis is an Octopus or Spider condition.
Mutable stays its own axis, so a Chameleon condition plays no part in Kraken.
A Spider reaches both the outgoing and the incoming limit.
A Chameleon reaches the mutable limit, an Octopus the outgoing limit.
A Centipede reaches the members limit, a Serpent the lines limit.

## `private static double TAuditGluedRead(List<TAuditObjectMember> members, int partCount, HashSet<TAuditObjectMember> excluded)`

Joins members linked by use into components and returns the share of parts the widest one spans.
Excluded members join nothing, so hub state can be lifted out before the read.
With nothing excluded it reads Glued, and with the hubs excluded it reads Fused.
