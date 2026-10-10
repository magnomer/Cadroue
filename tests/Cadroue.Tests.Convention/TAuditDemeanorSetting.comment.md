# TAuditDemeanorSetting.cs
Hash: `3eefb8a588774997`

## `internal static class TAuditDemeanorSetting`

Hand-written and tracked settings for the Demeanor driver pair of the border.
It holds the offer for `Cadroue.UIDemeanor>Cadroue.Conduct` alone.
No script writes this file.
`AuditStructure.json` holds its own copy, kept in step by hand as scripts/principles.md asks.
The border facts read it together with the other offer settings.
The offer now includes the courier, `CCourier` and `CCourierState`, beside the ledger.

## `public static readonly IReadOnlyDictionary<string, string[]> TAuditDemeanorOffer`

For the `ring>neighbour` pair, the neighbour types the ring may name at all.
Demeanor is offered exactly the public `C` types of Conduct, and no other Conduct type.
`TAuditDeportmentOffer` holds the same list in the same order for the other driver.
A change to the public `C` types of Conduct is made in both driver lists.
Each name stands as a literal, so the ratchet can read every entry.
