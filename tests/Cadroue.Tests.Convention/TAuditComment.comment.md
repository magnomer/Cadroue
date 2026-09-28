# TAuditComment.cs

## `public sealed class TAuditComment`

Keeps prose in the comment files and out of the sources.
Every rule value comes from `TAuditCommentSetting`, which mirrors scripts/auditcomments.json.
The findings match auditcomments.ps1 on the same tree, in the same order, though neither reads the other.

## `public void AuditComment_CommentLines_KeepLineRules()`

Reads every comment file under the configured roots, plus those of the listed root-level files.
A blank line is skipped, and a heading loses its `#` marks before it is judged.
Every line must be one sentence, hold at most the configured words, and carry none of the forbidden characters.
A heading counts only its words outside code spans.
Any hit names the file, line, and the rule it breaks.

## `public void AuditComment_Sources_CarryNoRemark()`

Scans every audited source and listed root-level file for the comment markers its extension declares.
String and character literals are blanked first, so a `//` inside a URL does not count.
Every line of an unclosed block comment counts until the line holding its closer.
Files named in the exempt list are skipped, root-level files too, since the generated registry carries a header.
Every extension with markers is scanned, whether or not it pairs with a comment file.

## `public void AuditComment_Sources_CarryCommentFile()`

Checks every configured source and listed root-level file has its paired comment file beside it.
The source list and generated-file exclusions match the script configuration.

## `public void AuditComment_CommentFiles_HaveSource()`

Every comment file under the roots must be the paired comment file of a source.
A comment file left behind by a deleted or renamed source is otherwise never noticed.

## `private static IEnumerable<string> TAuditOwnerRead(string repoRoot, string[] sources)`

Every source matching `sources` under the roots, plus the listed root-level files.

## `public void AuditComment_Headings_NameMembers()`

Every signature heading of a comment file names an identifier its source still holds.
It reads the same comment files as the line rules, so the root-level ones count too.
The source is the code or markup file the comment file sits beside, all of them when several share it.
A heading that is a markup snippet or a file name is left out.
A heading for a member that was renamed or deleted is otherwise never noticed.

## `private static IReadOnlyList<string> TAuditScanRead(string repoRoot, TAuditScope scope)`

Every tracked file in the scope, read through `TAuditSource`.
A configured root or root-level file that does not exist fails first, as it does in the script.
An empty scope fails rather than passing vacuously.

## `private static string? TAuditMemberRead(string span)`

The identifier a heading names: the last one before a parameter list, an initializer or a body.
Type arguments are dropped first.

## `private static string TAuditCommentRead(string path)`

Maps `.xaml.cs` to `.xaml.comment.md` and other sources to the matching `.comment.md` name.

## `private static readonly string[] TAuditCodeExtensions`

The files a comment file may describe: its stem itself, a source, a markup file or a code-behind.

## `private static readonly Regex TAuditHeadingPattern`

A second-level heading whose whole text is one code span.

## `private static readonly Regex TAuditFilePattern`

A heading that names a file rather than a member.

## `private static readonly Regex TAuditGenericPattern`

One innermost type argument list, dropped until none is left.

## `private static readonly Regex TAuditIdentifierPattern`

One identifier, verbatim or plain.

## `private static readonly Regex TAuditAbbreviationPattern`

A listed abbreviation with its full stop, which never ends a sentence.

## `private static readonly Regex TAuditLevelPattern`

The `#` marks that open a heading.

## `private static string TAuditLineCheck(string line)`

Returns the rule problems one line breaks, joined by a comma, or an empty string.
A leading list marker is dropped before the words are counted.
Code spans are dropped before the forbidden characters and sentence marks are looked for.
A sentence mark followed by any letter starts a new sentence, unless it closes an abbreviation.

## `private static string[] TAuditSourceRead(string path, bool code)`

Blanks every string literal over the whole file before splitting into lines.
An ordinary string or character literal never crosses a line break.
A verbatim or raw literal spanning lines keeps its line count, so a hit still reports the right line.
