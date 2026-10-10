# TAuditPlatformAnalyzer.cs
Hash: `398f4a2286265cce`

## `internal static class TAuditPlatformAnalyzer`

Decides whether the platform rule is an error for one source file of a portable half.
It resolves analyzer configuration files and project properties the way the compiler does.

## `private static readonly string[] TAuditConfigNames`

The analyzer configuration files the compiler reads from every folder above a project.

## `private static readonly Regex TAuditSectionPattern`

A section header of an analyzer configuration file, with its glob captured.

## `private static readonly Regex TAuditPairPattern`

A key and value line of an analyzer configuration file.

## `public static bool TAuditAnalyzerCheck(string repoRoot, string project, string source)`

True when the platform rule is an error for one source file.
Global configurations apply first, then editor configurations from the outermost to the nearest.
A folder above a `root = true` configuration is not read.
Only a section whose glob matches the file applies, and the last matching severity wins.
The rule escalates when a project file lists it as an error.
It also escalates when every warning is an error and the rule is not spared.
A severity of error always holds, a severity of warning holds only when escalated, and a lower severity never holds.
Without any severity, the rule holds only when escalated, since a warning is its default.

## `private static string? TAuditLevelRead(string config, string source, string key)`

The last value one configuration gives the key for the file, or null when it gives none.
A global configuration applies to every file.

## `private static bool TAuditGlobCheck(string glob, string relative)`

True when an editor configuration glob matches the file's path relative to the configuration.
A glob without a slash matches the file in any folder below.
