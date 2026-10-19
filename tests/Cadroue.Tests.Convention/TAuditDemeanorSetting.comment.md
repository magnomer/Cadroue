# TAuditDemeanorSetting.cs
Hash: `9c06247eb6fd2d9f`

## `internal static class TAuditDemeanorSetting`

Hand-written and tracked settings for the second driver pair of the border.
It holds the offer for `Cadroue.UIDemeanor>Cadroue.Conduct` alone.
No script writes this file.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.
The border facts read it together with the other offer settings.
`Cadroue.UIDemeanor` is a project that holds no source yet, so the row matches no source.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditDemeanorOffer`

For the `ring>neighbour` pair, the neighbour types the ring may name at all.
The list holds 31 public `C` types of Conduct.
They are the Atelier, Usher, lineage, workspace, placement, info, inspector, viewer, edit and Encoder types.
Conduct declares further public `C` types, such as CFlow and CRoster, that the list leaves off.
`TAuditDeportmentOffer` holds the same list for the other driver.
A public `C` type added to Conduct is listed in both driver offers.
Each name stands as a literal, so the ratchet can read every entry.
