# TAuditTruthLedger.json

The driver audit's ceilings, by kind and then by repo-relative file.
Each ceiling is the count of that kind in that file, and may only fall.
A file missing from a kind has a ceiling of zero.
`TAuditRatchet` holds every ceiling against the committed copy, and a new generation baselines it afresh.
When a count falls, `AuditTruth_Ledger_MatchesHits` writes the lowered ledger under `temp/audit` to copy here.
