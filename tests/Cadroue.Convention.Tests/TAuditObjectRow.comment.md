# TAuditObjectRow.cs

## `internal sealed record TAuditObjectRow`

One type after its parts are merged, with the numbers the facts and the report read.
`TAuditObjectParts` lists the repo-relative files declaring the type.
`TAuditObjectLines` sums the declaration spans of every part.
`TAuditObjectState` counts the fields, field-like events and auto-properties.
`TAuditObjectHubs` names each state slot reached from many parts, with its reach.
`TAuditObjectCross` counts the member references crossing from one part into another.
`TAuditObjectWeave` is the share of parts the largest member component spans.
`TAuditObjectFree` is the same share once hub state is removed.
`TAuditObjectDensity` is the cross references per member.
`TAuditObjectMonolith` is the verdict the settings floors give.
