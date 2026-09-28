# TAuditStrictLedger.json

The surface audit's ceilings, by kind and then by repo-relative file.
The surface kinds, the driver statics and the host wiring share this one ledger.
The driver kinds `Pack` and `Scaffold` started at the counts of generation 15.
The Veneer rows left with the Great Purge, and a returned file starts at zero.
Each ceiling is the count of that kind in that file, and may only fall.
A file missing from a kind has a ceiling of zero, so a new UITerminal file starts clean.
`TAuditRatchet` holds every ceiling against the committed copy, and a new generation baselines it afresh.
When a count falls, `AuditStrict_Ledger_MatchesHits` writes the lowered ledger under `temp/audit` to copy here.
