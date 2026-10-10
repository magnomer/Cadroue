# TAuditObjectRow.cs
Hash: `24db2a72e938da7f`

## `internal sealed record TAuditObjectRow`

One type after its parts are merged, with the numbers the facts and the report read.
`TAuditObjectParts` counts the declarations of the type.
A part is one declaration, so a file holding two declarations of the type counts twice.
`TAuditObjectLines` sums the declaration spans of every part, so the lines of a nested type count toward it.
`TAuditObjectMembers` counts the distinct members the parts declare, nested types left out.
A partial method or property counts once in Members.
`TAuditObjectMutable` counts the slots the type can rebind after construction.
Those are the fields neither constant nor readonly, and the backed properties with a non-init setter.
A readonly field of any type, an event, and a get-only or init-only property are not mutable.
A captured primary-constructor parameter is not mutable either, a known limit, though the compiler stores it.
`TAuditObjectOutgoing` counts the distinct codebase types this type names, its fan-out.
Outgoing counts types used, not slots, so a get-only, init-only or event declaration still adds its type.
`TAuditObjectIncoming` counts the distinct codebase types that name this type, its fan-in.
Incoming counts types, not members or references, so it stays in the unit of Outgoing.
`TAuditObjectHubs` names each hub slot with how many parts reach it.
`TAuditObjectShared` counts those hub slots.
`TAuditObjectCrossings` counts the member links crossing from one part into another.
`TAuditObjectGlued` is the share of parts the widest member component spans.
`TAuditObjectFused` is the same share once hub state is removed.
`TAuditObjectDensity` is the crossings per member.
`TAuditObjectFlags` lists every flag the type hits, in ladder order from Hydra down to Serpent.
A hub is a finding on a slot, so it is never among the flags.
`TAuditObjectVerdict` is the first flag, else Colony for several parts and Hermit for one.
A type whose only finding is a hub is therefore a Colony.
