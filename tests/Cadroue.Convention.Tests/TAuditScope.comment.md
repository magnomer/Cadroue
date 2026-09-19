# TAuditScope.cs

## `internal sealed record TAuditScope`

The file set one audit enumerates, built by that audit from its own sidecar.
Roots keep only paths under the listed folders, or every path when empty.
Include lists the `git ls-files` patterns.
Segments, Suffixes, and Prefixes drop directory names, file-name endings, and file-name starts.
Files drops exact file names, which is how an audit leaves its own tooling out.
