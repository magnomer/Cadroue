# TAuditEncodingSetting.cs

## `internal static class TAuditEncodingSetting`

Hand-written and tracked: the kinds of file the encoding facts read, the names they skip and the patterns they match.
No script writes this file.
Every pattern is spelled in escapes, so this file passes its own audit however it is copied.

## `public static readonly string[] TAuditEncodingInclude`

The tracked text kinds, as Git pathspecs: sources, markup, prose, settings, projects and scripts.

## `public static readonly string[] TAuditEncodingSkip`

The tracked files left out by name.
`version.json` is set by hand at each commit and its final newline is not the tooling's to demand.

## `public static readonly string[] TAuditEncodingTabless`

The suffixes of the files where a tab is never written, so a raw one is a paste gone wrong.

## `public const string TAuditEncodingControl`

The C0 controls other than tab, line feed and carriage return, and delete.

## `private const string TAuditEncodingTrail`

One byte of a multibyte sequence, as the character it becomes on the Windows-1252 page.
The undefined slots of that page are left out, as they never come back as a character.

## `public const string TAuditEncodingMojibake`

A byte order mark read as three characters, or two doubled characters in a row.
A three-byte lead followed by two trailing characters, twice, is one CJK or Hangul pair gone through the page.
A two-byte lead followed by one trailing character, twice, is a pair of accented letters gone the same way.
