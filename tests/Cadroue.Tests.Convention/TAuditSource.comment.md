# TAuditSource.cs

## `internal static class TAuditSource`

Shared file enumeration for every convention test.
Carries no settings of its own: each test hands in the `TAuditScope` its sidecar describes.

## `public static string TAuditRootRead()`

Walks up from the test binary to the first folder that holds `.git`.

## `private const string TAuditVersionFile = "version.json";`

The file the current version is read from, relative to the repo root.

## `public static string TAuditVersionRead(string repoRoot)`

The current version, for naming a report.

## `public static IReadOnlyList<string> TAuditFileRead(string repoRoot, TAuditScope scope)`

Asks Git for the tracked and untracked files matching the scope's patterns without case.
Git's output is read as UTF-8, so a path with non-ASCII letters is found, not skipped.
Ignored files never appear, and a file deleted on disk is skipped.
A path Git lists twice, as an unmerged path is, is kept once, compared without case.
The rest is filtered by the scope and sorted for a stable report.

## `private static bool TAuditExcludedCheck(string relativePath, TAuditScope scope)`

Applies the roots, segments, files, suffixes, and prefixes of the scope in that order.
Roots, segments, files and suffixes compare without case, as the audit scripts do.
