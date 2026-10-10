# TAuditBorder.cs
Hash: `981763c17e641645`

## `public sealed class TAuditBorder`

Keeps every ring inside its border: which ring a ring may name.
A ring names its one neighbour, ferries the data of any deeper ring and names nothing outward.
Above the cut, a UI ring names only its neighbour, data included.
A pair is counted in names, so a file already over its border cannot add names unseen.
The same border is audited by `AuditStructure.ps1` from its own configuration, and neither reads the other.

## `private const string TAuditBorderAudit = "AUDITBORDER";`

The audit name every report line opens with.

## `private static readonly string[] TAuditBorderHeld`

The kinds a ceiling is written for.

## `private static readonly Lazy<IReadOnlyList<TAuditHit>> TAuditBorderHits`

The tree, offer, seal, drift and linger hits, bound once and shared by every fact.

## `public void AuditBorder_Rings_NameOneNeighbour()`

No ring names more than one neighbour or itself, and every neighbour named is declared.
A capsule must be declared, have no neighbour and belong to one ring alone.

## `public void AuditBorder_Cut_MatchesShellRoots()`

The cut holds exactly the shell and capsule rings.

## `public void AuditBorder_Sources_HoldNoLeapfrogging()`

No pair holds more `Leapfrogging` names, behaviour past the neighbour, than its ceiling.
A method called on a deeper record is behaviour, even though the record is data.

## `public void AuditBorder_Sources_HoldNoTrespassing()`

No pair holds more `Trespassing` names, from a ring outside its closure, than its ceiling.

## `public void AuditBorder_Sources_HoldNoUndercutting()`

No pair holds more `Undercutting` names a UI ring takes from below the cut than its ceiling.
A `using` of a namespace below the cut counts as a name.

## `public void AuditBorder_OfferSignatures_HoldNoLeaking()`

No pair holds more `Leaking` offer signature types from below Conduct than its ceiling.
Methods, operators, conversions, properties, events, fields and nested public types are all read.

## `public void AuditBorder_SealedTypes_HoldNoUnsealing()`

No pair holds more `Unsealing` sealed member types from below the neighbour than its ceiling.
The ceilings fall as each controller is sealed, until none is left to count.

## `public void AuditBorder_Sources_HoldNoPoaching()`

Where an offer is written for a pair, no `Commuting` name outside it is named.
A neighbour name outside the offer is `Poaching`.

## `public void AuditBorder_Offers_HoldNoDrifting()`

No offer list drifts from its neighbour, so a renamed, hidden or new type cannot slip past the offers.
`Drifting` is hard, with no ceiling and no ledger.
Each hit names the pair, the entry or missing type, and the clause it failed.
An exempt row clears a hit as it clears every other border hit.

## `public void AuditBorder_Subscriptions_HoldNoLingering()`

No Conduct type leaves an engine event handler attached with no matching removal.
An engine-lifetime source outlives the Conduct object, so a kept handler keeps the object alive and firing.
`Lingering` is hard, with no ceiling and no ledger.
Each hit names the subscribing type, the event and the handler.
An exempt row clears a hit as it clears every other border hit.

## `public void AuditBorder_DriftedOffer_ReportsEachBreak()`

Proves the drift comparison fires on a stale, an internal, a wrong-prefix and a missing name.
Each break gives exactly one hit, so no clause reports an entry another clause already caught.

## `public void AuditBorder_EqualOffer_ReportsNoDrifting()`

Proves two cut pairs whose lists equal the neighbour's public types give no hit.
The lists differ only in order, since an offer is compared as a set.

## `public void AuditBorder_Ceiling_MatchesHits()`

No ceiling sits above its count, so a shed name lowers its ceiling.

## `public void AuditBorder_Exempt_MatchesSource()`

Every exempt row still matches a hit, so a fixed break deletes its row.
