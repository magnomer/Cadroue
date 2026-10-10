# TAuditRatchetFile.cs
Hash: `6b61da1b2c644e83`

## `internal static class TAuditRatchetFile`

Finds and reads the files `TAuditRatchet` holds, in the working tree and at HEAD through Git.

## `public const string TAuditSettingSuffix`

The file name ending of a held settings file.

## `public const string TAuditLedgerSuffix`

The file name ending of a held ledger.

## `public static readonly string TAuditSettingFolder`

The test project folder, named from the project so this file is the same in every project.

## `public static IReadOnlyList<string> TAuditTreeRead()`

The held file names in the working tree.

## `public static IReadOnlyList<string> TAuditHeadRead()`

The held file names at HEAD, failing when Git cannot list them.

## `private static bool TAuditHeldCheck(string name)`

True for a settings file or a ledger of the convention tests.

## `public static string TAuditWorkingRead(string name)`

The working copy of one held file.

## `public static string? TAuditCommittedRead(string path)`

The file as `git show HEAD:` prints it, or null when it is not in HEAD.
`TAuditCommentSnapshot` reads it too, to learn whether HEAD already carries a new stamp.

## `private static string? TAuditGitRead(params string[] arguments)`

The output of one Git command, or null when it fails.
