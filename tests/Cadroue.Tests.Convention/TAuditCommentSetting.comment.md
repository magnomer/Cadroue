# TAuditCommentSetting.cs
Hash: `3fc5b50f9dae1239`

## `internal static class TAuditCommentSetting`

Hand-written and tracked.
The comment-audit rules and scope live here, not in a generated sidecar.
AuditComments.ps1 reads its own tracked AuditComments.json and never writes this file.
The audits read no script configuration, and both copies change together as scripts/principles.md asks.

## `public static readonly string[] TAuditCommentRoots`

The folders searched for sources and their comment files.
They are Cadroue's own trees: the localization catalogs, the sources and the tests.

## `public static readonly string[] TAuditCommentExempt`

TAuditNameRegistry.cs and TAuditVerbRegistry.cs are generated and carry a generated-file header, so they alone are exempt.

## `public static readonly string[] TAuditCommentReserved`

Comment files only the developer edits, matching `exempt.comments` in the script configuration.
No line rule, heading or hash check reads them.
Each path is relative to the repository root and must exist.

## `public static readonly string[] TAuditCommentSources`

Lists the source extensions that need paired comment files.
Its patterns match the source pairs in scripts/AuditComments.json.

## `public static readonly string[] TAuditCommentAbbreviations`

Abbreviations whose full stop ends no sentence, matching `rules.abbreviations` in the script configuration.

## `public static readonly Dictionary<string, string[]> TAuditCommentMarkers`

The comment markers per source extension, matching `remark.markers` in the script configuration.

## `public static readonly IReadOnlyDictionary<string, int> TAuditCommentCeiling`

The count of comment files with no hash that may stand as a warning, matching `ceilings.unstamped`.
It caps the backlog of comment files not yet stamped, a backlog that may only shrink.
A count above it fails, and a ceiling above the count is stale and fails too.
Lower it with each stamp, never raise it to admit a new unstamped file.

## `public static readonly Dictionary<string, string> TAuditCommentClosers`

The closer of each block marker, matching `remark.closers` in the script configuration.
