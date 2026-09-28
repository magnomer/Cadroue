# TAuditLedger.cs

## `internal static class TAuditLedger`

Reads a ledger and holds walk hits against it.
A ledger keys a ceiling by kind and by file, so a hit is held where it lies.
A fix in one file never makes room for a regression in another, nor in the other medium.
A file the ledger does not name has a ceiling of zero, so new code starts clean.
Swapping one hit for another inside one file still passes, which is the ledger's grain.

## `private const string TAuditLedgerProposal = "temp/audit/{0}.json";`

Where a lowered ledger is written when ceilings went stale.

## `public static IReadOnlyDictionary<string, Dictionary<string, int>> TAuditLedgerRead(string file)`

The ledger's ceilings by kind, then by repo-relative path.

## `public static int TAuditLedgerCheck(`

Fails when any file holds more hits of the kind than its ceiling, listing each such file's hits.
Returns the kind's total so the caller can print it.
When `enforced` is false the check reports and passes.

## `public static void TAuditStaleCheck(`

Fails when any ceiling sits above its count, or when the ledger names a kind the audit does not count.
The lowered ledger is written so it can replace the tracked one in one copy.

## `private static string TAuditProposalSave(`

Writes the ledger the counts justify, never above the current ceilings.
Each count is capped at its ceiling, so a file over its ceiling is never admitted by the copy.
Returns the repo-relative path of the written file.
