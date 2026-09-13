# TAuditCommentSetting.cs

## `internal static class TAuditCommentSetting`

Hand-written and tracked: the comment-audit rules and scope live here, not in a generated sidecar.
auditcomments.ps1 reads its own gitignored auditcomments.json and never writes this file.
Keep the two in agreement by hand, because the tests must depend on nothing untracked.

## `public static readonly string[] TAuditCommentExempt`

TAuditNameRegistry.cs is generated and carries a generated-file header, so it alone is exempt.
