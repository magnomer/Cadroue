# TAuditNameSetting.cs

## `internal static class TAuditNameSetting`

Hand-written and tracked: every naming-audit setting lives here and nowhere else.
No script writes this file, and no registered name may appear in it.
Bases, verbs, and exemptions belong to docs-internal and reach the tests only through TAuditNameRegistry.cs.
The tests read nothing untracked, so a gitignored json or ps1 can never change what they check.
Run syncnames.ps1 before the tests so the registry they compile against matches docs-internal.
