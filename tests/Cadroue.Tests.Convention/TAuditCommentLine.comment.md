# TAuditCommentLine.cs
Hash: `cc1bd66f2202a39b`

## `internal static class TAuditCommentLine`

Judges one comment file line against the line rules of `TAuditComment`.
The word limit, forbidden characters, sentence marks and abbreviations all come from `TAuditCommentSetting`.

## `private static readonly Regex TAuditAbbreviationPattern`

A listed abbreviation with its full stop, which never ends a sentence.

## `private static readonly Regex TAuditLevelPattern`

The `#` marks that open a heading.

## `public static string TAuditLineCheck(string line)`

Returns the rule problems one line breaks, joined by a comma, or an empty string.
A leading list marker is dropped before the words are counted.
Code spans are dropped before the forbidden characters and sentence marks are looked for.
A sentence mark followed by a space and a letter starts a new sentence, unless it closes an abbreviation.
