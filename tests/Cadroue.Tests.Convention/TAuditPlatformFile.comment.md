# TAuditPlatformFile.cs
Hash: `0470f513ed276337`

## `internal static class TAuditPlatformFile`

Finds and reads the files the platform audit inspects, for `TAuditPlatform` and its helpers.
A file that cannot be read is recorded here, so the ceiling fact can list every one.

## `public static readonly string[] TAuditImportNames`

The files MSBuild imports on its own from every folder above a project.

## `public static readonly SortedDictionary<string, string> TAuditUnreadable = new(StringComparer.Ordinal);`

Every file that could not be read, keyed by its full path, with the reason.
A file read many times is still one unreadable file.

## `public static IEnumerable<string> TAuditImportRead(string repoRoot)`

The tracked `Directory.Build` imports anywhere in the tree.

## `public static string[] TAuditTextRead(string path)`

The lines of one file, or none when it cannot be read.
An unreadable file is recorded rather than thrown, so the fact lists every one.

## `public static IEnumerable<string> TAuditChainRead(string repoRoot, string project, string[] names)`

The named files from the project's folder up to the repository root, nearest first.

## `public static string TAuditRelativeRead(string repoRoot, string path)`

A path relative to the repository root with forward slashes.
