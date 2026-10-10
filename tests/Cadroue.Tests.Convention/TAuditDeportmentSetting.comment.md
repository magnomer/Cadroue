# TAuditDeportmentSetting.cs
Hash: `1ab283a12a526ea7`

## `internal static class TAuditDeportmentSetting`

Hand-written and tracked settings for the Deportment driver pair of the border.
It holds the offer for `Cadroue.UIDeportment>Cadroue.Conduct` alone.
No script writes this file.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.
The border facts read it together with the other offer settings.
The offer now includes the courier, `CCourier` and `CCourierState`, beside the ledger.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditDeportmentOffer`

For the `ring>neighbour` pair, the neighbour types the ring may name at all.
Deportment is offered exactly the public `C` types of Conduct, and no other Conduct type.
It ends with `CCompass`, `CDisplayCard` and `CDisplayRoute`, the display types Deportment names.
`TAuditDemeanorOffer` holds the same list in the same order for the other driver.
A change to the public `C` types of Conduct is made in both driver lists.
Each name stands as a literal, so the ratchet can read every entry.
