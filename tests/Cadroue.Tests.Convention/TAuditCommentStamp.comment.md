# TAuditCommentStamp.cs
Hash: `23c317818eaca20d`

## `public sealed class TAuditCommentStamp`

Holds every comment file to the hash of the sources it describes.
A stamp is a person's word that the prose was reread against the current source.
The findings match the hash rows of AuditComments.ps1, though neither reads the other.
The comment files come from `TAuditCommentFile`, the same scope `TAuditComment` judges.

## `public TAuditCommentStamp(ITestOutputHelper output)`

Keeps the test output sink so a fact can write its findings beside the failure.

## `public void AuditComment_StampedFiles_MatchSource()`

A comment file that carries a hash must carry the hash of its paired sources.
A source edit thus flags its comment file until a person rereads the prose and restamps it.
A stale hash always fails, whatever the ceiling.
Restamping is done one file at a time with scripts/StampComment.ps1.
The failure message spells out the fix.
Diff the sources, reread the touched sections, revise, then stamp.
A bare restamp is the evasion it warns against.

## `public void AuditComment_RestampedFiles_ReviseProse()`

Flags comment files restamped while their prose still matches the text they held when stale.
`TAuditCommentSnapshot` keeps that text between runs.
The hits go to the test output under a capitalized warning, and the fact still passes.
The script prints the same list as a `WARN` row.
Each side keeps its own snapshot, so each flags only the stale files it saw itself.

## `public void AuditComment_UnstampedFiles_HoldWithinCeiling()`

Comment files with no hash are backlog from before the stamp existed.
They pass as a warning while their count sits at or below the `Unstamped` ceiling.
The warning and its full list go to the test output, where the script prints a `WARN` row.
A count above the ceiling fails, so a new comment file must be stamped when it is written.

## `public void AuditComment_UnstampedCeiling_MatchesCount()`

The `Unstamped` ceiling equals the count of unstamped comment files.
A ceiling left above the count is stale and fails, so each stamp lowers it on both sides.

## `private static List<string> TAuditStampRead(string repoRoot)`

Every paired comment file whose second line holds no hash or a hash its sources no longer match.
A missing line reads as no hash, a wrong value as a changed source.

## `private static List<(string, string, string)> TAuditStateRead(string repoRoot)`

Every paired comment file with its second line and the hash its sources call for.
The comment files come from the owner list, so the root-level ones count too and come last.
A comment file whose source is exempt is skipped, as for headings.
A reserved comment file is skipped too, since only the developer edits it.

## `private const string TAuditMissingProblem`

The problem text of a comment file with no hash, as the script prints it.

## `private const string TAuditChangedProblem`

The problem text of a comment file whose hash its sources no longer match.

## `private const string TAuditUnstampedKind`

The ceiling key of the unstamped backlog, matching `ceilings.unstamped` in the script configuration.

## `private readonly ITestOutputHelper _tAuditOutput`

The test output that carries the backlog warning, since a passing fact shows no message.

## `private static string TAuditHashRead(IEnumerable<string> owners)`

The first 16 lowercase hex digits of the SHA-256 of the owners' text.
Each owner is read without its byte order mark, and CRLF or CR becomes LF.
The owners arrive in ordinal file-name order, so the script and the stamp tool join them alike.
Git line-ending settings or a downloaded copy therefore never change the value.

## `private static readonly Regex TAuditHashPattern`

The whole hash line.
It is `Hash:`, one space, then 16 lowercase hex digits in a code span.
