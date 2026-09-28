# TAuditEncoding.cs

## `public sealed class TAuditEncoding`

Keeps every tracked text file in one form: UTF-8 with no byte order mark and LF line breaks.
A tool writing through the wrong code page leaves a mark, a raw control byte or doubled characters.
Each fact reads the raw bytes, never a decoded string, so the decoder cannot hide what it repaired.
The kinds of file, the skipped names and the patterns live in `TAuditEncodingSetting`.

## `private static readonly UTF8Encoding TAuditEncodingStrict`

A decoder that throws on an invalid sequence instead of writing a replacement character.

## `public void AuditEncoding_TrackedText_DecodesAsUtf8()`

Decodes every file strictly and names the ones with a byte sequence that is not UTF-8.
The reason gives the offset of the first bad byte, so the counterpart script prints the same line.

## `public void AuditEncoding_TrackedText_HoldsNoMark()`

Names every file with a byte order mark anywhere, the first three bytes included.
The repository writes UTF-8 without a mark, as `.editorconfig` declares for every editor.

## `public void AuditEncoding_TrackedText_BreaksLinesWithLf()`

Names every file holding a carriage return, whether paired with a line feed or alone.
The repository breaks lines with LF alone, in Git and in the working tree, as `.gitattributes` declares.

## `public void AuditEncoding_TrackedText_EndsWithNewline()`

Names every non-empty file whose last byte is not a line feed.

## `public void AuditEncoding_TrackedText_HoldsNoControl()`

Names every file holding a raw control character other than tab, line feed and carriage return.
A test that needs one writes the escape, so the character is visible in the source.

## `public void AuditEncoding_TrackedText_HoldsNoMojibake()`

Names every file holding text that was UTF-8 once and was read back as a single-byte page.
A run of two doubled characters is required, so a stray accented letter before a quote passes.

## `public void AuditEncoding_Sources_HoldNoTab()`

Names every source or markup file holding a tab, which the editors here never write.
A tab inside a literal is written as its escape.

## `public void AuditEncoding_Scripts_HoldOnlyAscii()`

Names every PowerShell script holding a byte outside ASCII, with the line of the first one.
Windows PowerShell 5.1 reads a script without a mark in the ANSI page, so any other byte changes meaning.
A script that needs such a character builds it from its code.

## `private static string? TAuditLineFind(byte[] bytes, Regex pattern, string label)`

The label and line of the first match, or nothing.

## `private static List<string> TAuditEncodingScan(Func<string, byte[], string?> verdict)`

Reads every tracked file of the audited kinds and collects each verdict that is not nothing.
Fails when Git enumerates no file, so an empty scope cannot pass vacuously.
