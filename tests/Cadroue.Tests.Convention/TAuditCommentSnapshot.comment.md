# TAuditCommentSnapshot.cs
Hash: `c0bf23ddaee7edd0`

## `internal static class TAuditCommentSnapshot`

Catches a hash bumped without a revision of the prose it vouches for.
It mirrors the snapshot of scripts/AuditComments.ps1 but keeps its own file, so neither side reads the other.
A stale file seen by only one side is therefore flagged by that side alone.

## `private const string TAuditSnapshotFile`

The snapshot sits under `obj`, which git ignores, so it stays local to one working tree.
Each entry maps a comment file to its stale second line and its full text at that time.

## `public static List<string> TAuditRestampRead(string repoRoot, IEnumerable<(string, string, string)> states)`

Records every stale comment file once per stale stamp, keeping the text from the first run that saw it.
A later run that finds the file current again compares its prose with that text.
Unchanged prose is a hit, unless HEAD already carries the new stamp.
An entry clears when the prose changes or the stamp is committed.
It also clears when the file loses its hash or its source.
A source edited back to its stamped state clears the entry too.
The snapshot is rewritten on every run with only the entries still live.

## `private static Dictionary<string, string[]> TAuditSnapshotRead(string snapshotPath)`

A missing or broken snapshot reads as empty, so the first run starts clean.

## `private static string TAuditProseRead(string text)`

The prose without the Hash line, with every line trimmed, spacing squeezed and blank lines dropped.
A respaced file thus still counts as unchanged.
