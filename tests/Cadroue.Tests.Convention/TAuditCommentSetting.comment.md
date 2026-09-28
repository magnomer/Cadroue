# TAuditCommentSetting.cs

## `internal static class TAuditCommentSetting`

Hand-written and tracked: the comment-audit rules and scope live here, not in a generated sidecar.
auditcomments.ps1 reads its own gitignored auditcomments.json and never writes this file.
Keep the two in agreement by hand, because the tests must depend on nothing untracked.

## `public static readonly string[] TAuditCommentFiles`

Single root-level sources audited beside the roots, so their in-code comments are caught.
The script refuses an empty list, and each of these already has a comment file.

## `public static readonly string[] TAuditCommentSources`

The source patterns that must carry a comment file beside them.
A comment file is secondary here, written where prose helps and never required of code.
An empty list would match every file, so only project and data files are listed.

## `public static readonly string[] TAuditCommentOptional`

The source patterns a comment file may pair with without being required.
A comment file beside one of these has a source, but a source without one is no finding.
This setting exists only in Cadroue, where comment files on code are optional.

## `public static readonly string[] TAuditCommentExempt`

TAuditNameRegistry.cs is generated and carries a generated-file header, so it alone is exempt.
