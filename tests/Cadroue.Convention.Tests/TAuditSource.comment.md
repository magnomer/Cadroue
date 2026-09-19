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

Asks Git for the tracked and untracked files matching the scope's patterns.
Ignored files never appear, and a file deleted on disk is skipped.
The rest is filtered by the scope and sorted for a stable report.

## `private static bool TAuditExcludedCheck(string relativePath, TAuditScope scope)`

Applies the roots, segments, files, suffixes, and prefixes of the scope in that order.
