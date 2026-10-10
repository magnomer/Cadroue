# TAuditCommentFile.cs
Hash: `3f37279d0bf773ed`

## `internal static class TAuditCommentFile`

Finds the sources and comment files the comment audit inspects, for `TAuditComment` and `TAuditCommentStamp`.
Both read one scope, so the line rules and the hash stamps judge the same files.

## `public static IEnumerable<string> TAuditOwnerRead(string repoRoot)`

Every source that needs a comment file.
That is the paired sources under the roots and the listed root-level files.

## `public static IReadOnlyList<string> TAuditScanRead(string repoRoot, TAuditScope scope)`

Every tracked file in the scope, read through `TAuditSource`.
A configured root, root-level file or reserved comment file that does not exist fails first, as in the script.
An empty scope fails rather than passing vacuously.

## `public static bool TAuditReservedCheck(string repoRoot, string path)`

Whether the path names a comment file listed in `TAuditCommentReserved`.
Only the developer edits such a file, so the line rules, headings and hash stamps skip it.

## `public static string TAuditCommentRead(string path)`

Maps `.xaml.cs` to `.xaml.comment.md` and other sources to the matching `.comment.md` name.
