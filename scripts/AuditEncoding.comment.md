# AuditEncoding.ps1
Hash: `3f18f39720d7d14e`

Checks every tracked text file for one encoding form, without the convention tests.
It is the standalone counterpart of `TAuditEncoding` and reports the same hits.
It runs in plain PowerShell with git alone, so it answers in a second or two.

## Usage

```
AuditEncoding [-Root <path>] [-ConfigPath <path>] [-Open] [-NoOpen] [-Help]
```

`-Root` audits another git working tree with the same configuration.
`-ConfigPath` reads another configuration instead of `AuditEncoding.json`.
`-Open` opens the Markdown report after the run.
`-NoOpen` writes the page without opening it.

## Scope

Files come from `git ls-files --cached --others --exclude-standard`, as in the test.
Each include is a git pathspec matched without case.
A file under an excluded segment or with a skipped name drops out.
Ignored files are never read, so the local scripts outside the allowlist stay out of scope.
Files sort by full path, ordinal and without case, as the test sorts them.
A path git lists twice is read once.

## Checks

| Gate | Hit |
|---|---|
| Invalid UTF-8 | The offset of the first ill-formed byte sequence. |
| Byte order marks | The offset of the first mark, anywhere in the file. |
| Carriage returns | The number of carriage returns. |
| Missing final newlines | A non-empty file whose last byte is not a line feed. |
| Control characters | The line of the first raw control character. |
| Double-encoded text | The line of the first UTF-8 run read back through a single-byte page. |
| Tabs in sources | The line of the first tab in a file with a tabless suffix. |
| Non-ASCII scripts | The line of the first byte outside ASCII in a file with an ascii suffix. |

## Settings

`AuditEncoding.json` is copied by hand from `TAuditEncodingSetting`, and the two never read each other.
The mojibake pattern writes `{trail}` where the setting splices in its trail class.
The ascii suffixes carry the rule the test writes inline for PowerShell scripts.
The `report` block names the report folder, the version file, the version key and the file prefix.
Its paths resolve against the project root, and a missing version file falls back to `0.0.0`.

## Output

The console follows `report.md`, and every hit prints as `path: reason`.
The exit code is 1 when any Result gate is above 0 and 0 otherwise.

## Reports

Every run writes `{directory}/AuditEncoding-{version}.md` and `{directory}/AuditEncoding-{version}.html`.
The Markdown report holds the scope, the Result table and every hit list in console order.
Each hit list is headed with its count and reads `None.` when empty.
The page fills `AuditEncoding.html` at its `__TITLE__` and `/*__DATA__*/` markers.
It shows the verdict, the Result table, tiles, scope, extension totals, every hit list and every scanned file.
Its data is JSON written by hand in ASCII, so both PowerShell versions write the same bytes.
The page opens with `Start-Process` unless `-NoOpen` is given.
The console ends with one `Report:` line per written file, the Markdown report first.
The report folder lies inside git scope, so the Markdown report must itself pass this audit.

## Parity

The invalid offset is walked by hand, since .NET Framework counts its decoder index from an internal buffer.
The walk reports the start of the ill-formed sequence, as the .NET decoder does in the test.
Offsets and lines count bytes through the Latin-1 page, so each character is one byte.
