# TAuditLineSetting.cs

## `internal static class TAuditLineSetting`

Hand-written and tracked: the line-audit thresholds and scope live here, not in a generated sidecar.
auditlines.ps1 reads its own gitignored auditlines.json and never writes this file.
Keep the two in agreement by hand, because the tests must depend on nothing untracked.
