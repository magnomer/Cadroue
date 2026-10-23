# Object

This folder holds the object audit, which grades every merged type of the sources its settings include.
It also holds the audit's member, part, row and type records, its setting and its walker.

## `TAuditObject`

Grades every type on a ladder of creature verdicts, worst first, and records every flag it hits.
Each flag is measured on the merged type, so splitting a type into partial files escapes none.
The verdict is for display, and each flag is held to its own ceiling.
A Kraken therefore still counts under its Octopus or Spider flag, and fixing one axis lowers one counter.
While `TAuditObjectEnforced` is false, the flag, hub and part facts pass and only print their counts.
Enforced, such a fact fails when its count rises above its ceiling.
The stale-ceiling fact fails either way.
`AuditObject.ps1` is the counterpart script, and neither side reads the other.
On the same tree both reach the same results, and each side keeps its own presentation.
The only allowed difference in results is a config fault.
The script throws on any missing key in its json, a missing ceiling key included.
This test reads a missing ceiling key as 0, so that flag's fact fails once the flag has a hit.
A missing limit key throws on both sides.
The ladder runs Hydra, Kraken, Spider, Chameleon, Octopus, Centipede, Serpent, Colony, Hermit.
The first seven are flags, and the last two are the healthy verdicts of a type with no flag.
The walk runs once and every fact reads the same rows.
The report is written once, on the first fact that runs, so it exists whichever fact runs first.
A Hydra reaches the Parts and Lines limits, and the Fused or the Density limit.
It is one object behind many parts, big and tangled inside.
A Kraken hits the size axis and the coupling axis together.
The size axis is a Serpent or Centipede condition, the coupling axis an Octopus or Spider condition.
It is big and coupled outward, so no single move fixes it.
Mutable is its own axis, so a Chameleon condition never makes a Kraken.
A Spider reaches both the Outgoing and the Incoming limit, so it uses many types and is used by many.
A Chameleon reaches the Mutable limit and holds many slots it can rebind after construction.
An Octopus reaches the Outgoing limit, so it uses many other codebase types.
A Centipede reaches the Members limit, and a Serpent reaches the Lines limit over its merged parts.
A hub is a state slot reached from as many parts as the hub limit, or more.
It is a finding on the slot, outside the ladder, so it never sets a verdict.
Each hit names the owning type, which keeps a hub visible beside a healthy verdict.
A type whose only finding is a hub stays a Colony, with its hub count in the verdict table.
Every split type is declared in no more parts than its ceiling.
A split type without a ceiling may hold one part, so a new partial split fails at once.
A part row whose type is whole again counts as one part.
The report table lists every type with a flag, several parts or a hub, in ladder order.
A closing line counts the Hermits left out.
The report lands at the path the setting names, and its lines end in LF alone.
The head lists the generation, the switch, the type count and the count of each verdict.
Each flag and the hub follow against their ceilings, then the above and stale counts and the rules.
A part overrun is listed among the above-ceiling lines.
An empty enumeration fails rather than passing vacuously.
A list the binder walks none of fails too, as AuditObject.ps1 does.

## `TAuditObjectMember`

One member of a merged type, with the part declaring it and whether it holds state.
The index is its slot in the union-find that Glued and Fused are read from.
The state mark flags a slot a hub is looked for in.
The mutable mark flags a slot rebindable after construction.
The uses are the members of the same type its declaration reads, writes or calls.
The users are the reverse.

## `TAuditObjectPart`

One declaration of a type, with the lines it spans.
A part is compared by reference, so two declarations in one file stay two parts.

## `TAuditObjectRow`

One type after its parts are merged, with the numbers the facts and the report read.
A part is one declaration, so a file holding two declarations of the type counts twice.
Lines sums the declaration spans of every part, so the lines of a nested type count toward it.
Members counts the distinct members the parts declare, nested types left out.
A partial method or property counts once in Members.
Mutable counts the fields neither constant nor readonly, and the backed properties with a non-init setter.
A readonly field of any type, an event, and a get-only or init-only property are not mutable.
A captured primary-constructor parameter is not mutable either, a known limit, though the compiler stores it.
Outgoing counts the distinct codebase types this type names, its fan-out.
Outgoing counts types used, not slots, so a get-only, init-only or event declaration still adds its type.
Incoming counts the distinct codebase types that name this type, its fan-in.
Incoming counts types, not members or references, so it stays in the unit of Outgoing.
Hubs names each hub slot with how many parts reach it, and Shared counts those slots.
Crossings counts the member links crossing from one part into another, and Density is crossings per member.
Glued joins members linked by use into components and is the share of parts the widest one spans.
Excluded members join nothing, so hub state can be lifted out before the read.
With nothing excluded it reads Glued, and with the hubs excluded it reads Fused.
Flags lists every flag the type hits, in ladder order from Hydra down to Serpent.
The flags are read from the finished numbers.
A hub is a finding on a slot, so it is never among them.
The verdict is the first flag, else Colony for several parts and Hermit for one.

## `TAuditObjectSetting`

Hand-written and tracked, this file holds the object-audit settings grouped by verdict.
No script writes this file.
`AuditObject.ps1` keeps its own copy in AuditObject.json, and neither side reads the other.
Each threshold dictionary is named for its verdict or for the hub, and ends in `Limit`.
The ratchet holds every name ending in `Limit`, so its values may only fall.
Raising a limit spares more types, which would loosen the audit.
Every limit is reached at its value or above.
Kraken has no dictionary, since it reuses the Serpent, Centipede, Octopus and Spider limits.
The enforced switch false makes every flag, hub and part fact a warning that passes.
The report path carries the version.
The Hydra limit needs Parts and Lines, and Fused or Density too.
Fused is Glued once hub state is removed, so one shared field alone never makes a Hydra.
It holds doubles, since Fused is a share and Density is crossings per member.
A Spider needs both Outgoing and Incoming.
Incoming alone flags nothing, since a widely used record or helper is healthy.
The hub limit counts the declaring part as reaching the slot.
The ceiling holds the hit count each flag may reach, and the hub slots over every type.
A type counts once under every flag it hits, so a Kraken still counts as an Octopus or Spider.
A count above fails the fact, and a ceiling above the count is stale and fails too.
Lower a ceiling when a type sheds a flag, never raise one to admit a new one.
Each key is kept equal to the same key under `ceiling` in AuditObject.json, by hand.
The parts ceiling is keyed by full type name.
A type not listed may hold one part.
Dropping a row once its type is whole tightens, so the ratchet lets it go.
Lower a ceiling when a plan lifts a part into its own type, never raise one.
The include patterns are the `git ls-files` patterns of the sources the walk compiles.

## `TAuditObjectType`

A type under merge: its symbol, the parts found so far and every member keyed by symbol.
The member map is keyed with the symbol comparer, so a reference bound in any part finds its target.

## `TAuditObjectWalker`

Merges every partial type of the bound sources and measures how tightly its parts are glued.
It also measures how far each type couples to other codebase types, and how much state it rebinds.
Binding goes through `TAuditBinder`, the compilation every bound audit shares.
A first pass registers members and a second links them.
A third pass collects the type uses of every codebase type, member-less ones included.
Incoming is counted only after every use is known, since any type may name any other.
It returns one row per type with members, longest first, then by name.
A type without members gets no row, yet its uses still count toward the Incoming of others.
A partial method or property registers once, on the part that implements it.
The defining part of a partial method or property declares nothing when an implementing part exists.
The implementing part stands for it under the defining symbol, which references bind to.
A partial property is state and mutable as its implementing part is.
A nested type or delegate declares no member of the outer type.
A nested type is merged as its own type, and a delegate gets no row.
A member is never linked to itself, and an unresolved overload takes its first candidate.
An accessor resolves to its property or event.
A codebase type is any class, struct, interface, record, enum or delegate declared in a walked file.
Every name counts toward uses, in a signature, a member type or a body alike.
Names inside a nested type or delegate belong to that nested type, not to the outer one.
A type never uses itself, a type nested in it, or a type it is nested in.
Partial parts add to one set, so a use between parts of one type drops out.
An alias gives its target, an array its element and a pointer its pointed-at type.
A named type gives its original definition and every type in its arguments.
An anonymous type gives nothing, and a member gives only the original definition of its containing type.
Namespaces, locals, parameters and type parameters give nothing.
State is a field that is not a constant, a field-like event, or a property with no accessor body.
Mutable is a field neither constant nor readonly, or a property with a backing field and a non-init setter.
A static field counts like an instance field.
A readonly field never rebinds, so it is not mutable whatever its type holds.
A get-only or init-only property is set only during construction, so it is not mutable either.
An event only notifies, so it is not mutable.
A captured primary-constructor parameter is neither state nor mutable, a known limit, since it declares no member.

## Stamp

Commit: `90a13c9590ba`
- `TAuditObject.cs` `d7c49bde47bed70d`
- `TAuditObjectMember.cs` `a16c1d47d7995c6c`
- `TAuditObjectPart.cs` `8d291a83366d68f8`
- `TAuditObjectRow.cs` `4bab03b10990872d`
- `TAuditObjectSetting.cs` `f137dbd3fdb5a20e`
- `TAuditObjectType.cs` `b202f87d774b9373`
- `TAuditObjectWalker.cs` `30f80d6d1d86e20e`
