# TAuditTruthLedger.json
Hash: `34d67d5f25a939de`

The driver audit's ceilings, by kind and then by repo-relative file.
Every row is a Deportment file, since Deportment is Cadroue's only driver.
Each ceiling is the count of that kind in that file.
Every kind may only fall.
Mismatching holds no rows, since it only informs while `src/Cadroue.UIDemeanor` is absent.
Puppeteering holds no rows, since no Deportment type is a control type.
A file missing from a kind has a ceiling of zero.
`AuditUI.ledger.json` holds the same rows for the script, copied by hand.
`TAuditRatchet` holds every ceiling against the committed copy, and a new generation baselines it afresh.
When a count falls, `AuditTruth_Ledger_MatchesHits` writes the lowered ledger under `temp/audit` to copy here.
