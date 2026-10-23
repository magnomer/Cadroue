# Comment

This folder holds the comment audit keeping prose in sidecars.
It also holds its files, line judge, stamp comparison and setting.

## `TAuditComment`

Keeps prose in the sidecars and out of the sources.
Every rule value comes from `TAuditCommentSetting`, which mirrors scripts/AuditComments.json.
The findings match AuditComments.ps1 on the same tree, though neither reads the other.
`TAuditCommentFile` supplies the scope, `TAuditCommentLine` judges each line, and `TAuditCommentStamp` holds the stamp facts.
Every sidecar under the roots is read up to its `## Stamp` section, which is not prose.
A reserved sidecar is skipped, since only the developer edits it.
Every line must be one sentence, hold at most the configured words, and carry none of the forbidden characters.
Any hit names the file, line, and the rule it breaks.
Every audited source is scanned for the comment markers its extension declares.
In `.cs` files, string and character literals are blanked first, so a `//` inside a URL does not count.
A verbatim or raw literal spanning lines keeps its line count, so a hit reports the right line.
Every line of an unclosed block comment counts until the line holding its closer.
Exempt files are skipped.
Every folder holding a source needs its one sidecar.
A sidecar that is no folder's sidecar is left behind by a deleted or renamed folder and fails.
Every signature heading of a sidecar must name an identifier that a source in its folder still holds.
A heading that is a markup snippet is left out.
A heading that is a file name must name a file in the folder.
A heading names the last identifier before a parameter list, an initializer, a body or a base list.
Type arguments are dropped before the identifier is taken.

## `TAuditCommentFile`

Finds the sources and sidecars the comment audit inspects, for `TAuditComment` and `TAuditCommentStamp`.
Both take their roots and excluded segments from one setting, so they judge the same sidecars.
A configured root or reserved sidecar that does not exist fails first, as in the script.
An empty scope fails rather than passing vacuously.
The repository root is exempt, so a file directly inside it is never scanned.
The owners of a sidecar are the paired sources under the roots.
A sidecar sits in its source's folder as `<Folder>.comment.md`.
A reserved sidecar is one listed in `TAuditCommentReserved`.
Only the developer edits such a file, so every check skips it.
The prose of a sidecar ends before its `## Stamp` heading, so the stamp rows never meet the line rules.

## `TAuditCommentLine`

Judges one sidecar line against the line rules of `TAuditComment`.
The word limit, forbidden characters, sentence marks and abbreviations all come from `TAuditCommentSetting`.
It returns the rule problems one line breaks, joined by a comma, or an empty string.
A leading list marker is dropped before the words are counted.
A heading counts only its words outside code spans.
Code spans are dropped before the forbidden characters and sentence marks are looked for.
A sentence mark followed by a space and a letter starts a new sentence, unless it closes a listed abbreviation.

## `TAuditCommentSetting`

Hand-written and tracked, so the comment-audit rules and scope live here, not in a generated sidecar.
AuditComments.ps1 reads its own tracked AuditComments.json and never writes this file.
The audits read no script configuration, and both copies change together as scripts/principles.md asks.
The generation constant equals the script's generation, so a mismatch shows the two drifted.
`TAuditCommentEnforced` matches `enforced` in the script configuration.
While it is false, every finding fact passes and the script reports it as a warning.
Setting it false lets the audit land before the sidecars migrate.
Stale ceilings and unreadable files fail either way.
The exempt files are named in the setting and skipped by the source, heading and stamp checks.
Reserved sidecars match `exempt.comments` in the script configuration, and no check reads them.
Each path is relative to the repository root and must exist.
The source patterns match `sources.suffixes` in scripts/AuditComments.json.
The abbreviations, markers and closers match `rules.abbreviations`, `remark.markers` and `remark.closers`.
The unstamped ceiling matches `ceilings.unstamped`.
A count above it fails, and a ceiling above the count is stale and fails too.
Lower it with each stamp, never raise it to admit a new unstamped sidecar.
The folder file limit and the per-folder ceilings match the script's folder limit and its ceiling rows.
A ceiling only lowers a warning, and it sits above the limit and at or under its folder's count.

## `TAuditCommentStamp`

Compares each sidecar with the stamp it carries in its trailing `## Stamp` section.
The stamp holds a commit and one hash row per source.
A stamp is a person's word that the prose was reread against the sources.
The findings match the stamp rows of AuditComments.ps1, though neither reads the other.
The sidecars come from the owners that `TAuditCommentFile` finds.
The test output sink is kept so a warning lands beside the result.
A source changed, added or removed since the stamp only advises, never fails.
The advice names the diff to run against the stamped commit.
Hash drift stays advice because a source edit does not always touch the sidecar's intent.
A sidecar with no stamp or no commit is backlog from before the stamp existed.
It passes as a warning while the count sits at or below the `Unstamped` ceiling.
A count above the ceiling fails once enforced, so a new sidecar is stamped when written.
The ceiling equals the count, so each stamp lowers it on both sides.
A folder holding more sources than the folder file limit warns, and never fails.
A folder under a ceiling row warns only above that ceiling.
A ceiling row that sits above its count, or at or under the limit, warns as stale.
Both folder checks only warn, since a split costs a design decision.
A sidecar whose sources are all exempt is skipped, and so is a reserved one.
The hash is the first 16 lowercase hex digits of the SHA-256 of the source's text.
Each source is read without its byte order mark, and CRLF or CR becomes LF.
Git line-ending settings or a downloaded copy therefore never change the value.

## Stamp

Commit: `90a13c9590ba`
- `TAuditComment.cs` `75ff40287e6ac7b3`
- `TAuditCommentFile.cs` `2f985b13b8af43f1`
- `TAuditCommentLine.cs` `cc1bd66f2202a39b`
- `TAuditCommentSetting.cs` `22cd7836d2993aee`
- `TAuditCommentStamp.cs` `3cb323a65e5d3e86`
