# Line

This folder holds the file-form audits, each with its setting.
They enforce line and width limits and the UTF-8, no-BOM, LF encoding rule.

## `TAuditEncoding`

Keeps every tracked text file in one form, UTF-8 with no byte order mark and LF line breaks.
A tool writing through the wrong code page leaves a mark, a raw control byte or doubled characters.
Each fact reads the raw bytes, and besides the strict decode fact only the line finders decode them.
The strict decode fact guards that step, so a lenient decoder cannot hide what it repaired.
The kinds of file, the skipped names and the patterns live in `TAuditEncodingSetting`.
The strict decoder throws on an invalid sequence instead of writing a replacement character.
Strict decoding names every file with a byte sequence that is not UTF-8.
The reason gives the offset of the first bad byte, so the counterpart script prints the same line.
A byte order mark anywhere in a file is named, the first three bytes included.
Every file is written as UTF-8 without a mark.
A carriage return, paired with a line feed or alone, is named.
Every file breaks lines with LF alone.
A non-empty file whose last byte is not a line feed is named.
A raw control character other than tab, line feed and carriage return is named.
A test that needs one writes the escape, so the character is visible in the source.
Text that was UTF-8 once and was read back as a single-byte page is named.
A run of two doubled characters is required, so a stray accented letter before a quote passes.
A tab in a file of a tabless suffix is named.
A tab inside a literal is written as its escape.
A PowerShell script holding a byte outside ASCII is named with the line of the first one.
Windows PowerShell 5.1 reads a script without a mark in the ANSI page, so any other byte changes meaning.
A script that needs such a character builds it from its code.
The scan fails when Git enumerates no file, so an empty scope cannot pass vacuously.

## `TAuditEncodingSetting`

Hand-written and tracked, with no script writing it.
Every pattern is spelled in escapes, so this file passes its own audit however it is copied.
The included kinds are Git pathspecs.
The skipped names are exempt from every encoding fact.
The tabless suffixes mark files where a tab is never written, so a raw one is a paste gone wrong.
The trail character class is one byte of a multibyte sequence as it reads on the Windows-1252 page.
The undefined slots of that page are left out, as they never come back as a character.
The mojibake pattern is a byte order mark read as three characters, or two doubled characters in a row.
A three-byte lead followed by two trailing characters, twice, is one CJK or Hangul pair gone through the page.
A two-byte lead followed by one trailing character, twice, is a pair of accented letters gone the same way.

## `TAuditLine`

Holds every source file within the line limit and every line within the width limit of its extension.
A file or line in the band below a limit prints as a warning and does not fail.
Enforced, a kind fails when it counts above its ceiling.
Every hit, warning and ceiling matches AuditLines.ps1 on the same tree, though neither reads the other.
The walk runs once and every fact reads the same rows.
The runner's output is kept so a passing fact can still print its count and warnings.
A file above the line limit is a hit, largest first.
A line wider than the limit of its extension is a hit, named by file and line number.
An extension without a width limit is never checked.
Every ceiling equals its count, so a ceiling left above the count is stale.
That holds even when the line rules are not enforced.
The advice printed with a hit says to reconsider the code, never to split or wrap it mechanically.
A mechanical split or a partial part leaves one big object across files, a Serpent to the object audit.
It becomes a Hydra once it spreads.
Width is a sign of depth, chaining or a long path, so the code is reconsidered first.
A failure carries the advice before the hits.
A configured root without a directory fails first, as it does in the script.
An empty enumeration fails rather than passing vacuously.

## `TAuditLineRow`

One source file after it is read, with the numbers the facts read.
The width limit is the maximum integer when no limit applies to the extension.
The wide list holds each line inside the width band or above it, with its number and width.

## `TAuditLineSetting`

The line-audit thresholds, ceilings and scope live here, hand-written and tracked, not in a generated sidecar.
The line limit is the last count that passes, and the warning line is the last that passes silently.
Until a commit, the ratchet keeps the ceilings and limits from rising and `TAuditLineEnforced` from turning off.
Every other field here may not change at all until a commit.
AuditLines.ps1 reads its own tracked AuditLines.json and never writes this file.
The audits read no script configuration, and both copies change together as scripts/principles.md asks.

## Stamp

Commit: `90a13c9590ba`
- `TAuditEncoding.cs` `fc87c4dcc1a10651`
- `TAuditEncodingSetting.cs` `def9bacf5940d3c8`
- `TAuditLine.cs` `9b9bf5b3e3e16d9e`
- `TAuditLineRow.cs` `b7d6154dc7c2cfb1`
- `TAuditLineSetting.cs` `b7abb2330f29c3b6`
