# TAuditComment.cs

## `public sealed class TAuditComment`

Keeps prose in the comment files and out of the sources.
Every threshold comes from `TAuditCommentSetting`, hand-written tracked code that no script regenerates.

## `public void AuditComment_CommentLines_KeepLineRules()`

Reads every comment file under the configured roots, plus those of the listed root-level files.
A blank line or a heading is skipped.
Every other line must be one sentence, hold at most the configured words, and carry none of the forbidden characters.
Any hit names the file, line, and the rule it breaks.

## `public void AuditComment_Sources_CarryNoRemark()`

Scans every audited source and listed root-level file for the comment markers its extension declares.
String and character literals are blanked first, so a `//` inside a URL does not count.
Files named in the exempt list are skipped, since the generated registry carries a header.

## `public void AuditComment_Sources_CarryCommentFile()`

Checks every configured source and listed root-level file has its paired comment file beside it.
The source list and generated-file exclusions match the script configuration.

## `private static string TAuditCommentRead(string path)`

Maps `.xaml.cs` to `.xaml.comment.md` and other sources to the matching `.comment.md` name.

## `private static string TAuditLineCheck(string line)`

Returns the rule problems one line breaks, joined by a comma, or an empty string.
A leading list marker is dropped before the words are counted.
Code spans are dropped before the forbidden characters and sentence marks are looked for.

## `private static string[] TAuditSourceRead(string path, bool code)`

Blanks every string literal over the whole file before splitting into lines.
A verbatim or raw literal spanning lines keeps its line count, so a hit still reports the right line.
