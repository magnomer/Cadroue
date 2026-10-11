# TAuditStrictLedger.json
Hash: `bc3eba88669ef694`

The surface audit's ceilings, by kind and then by repo-relative file.
The surface kinds, the driver sharing and the host overworking all live in this one ledger.
The surface rows are Veneer files, the sharing rows Deportment files, and the overworking row the host.
Each ceiling is the count of that kind in that file, and may only fall.
A file missing from a kind has a ceiling of zero, so a new Veneer file starts clean.
`AuditUI.ledger.json` holds the same rows for the script, copied by hand.
`TAuditRatchet` holds every ceiling against the committed copy, and a new generation baselines it afresh.
When a count falls, `AuditStrict_Ledger_MatchesHits` writes the lowered ledger under `temp/audit` to copy here.
