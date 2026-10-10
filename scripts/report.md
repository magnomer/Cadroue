# Audit console report grammar

This file is the single layout every audit script in `scripts/` prints to the console.

## Order

The report runs from the widest view to the narrowest.
An audit prints the levels it has in this order and skips the ones it lacks.

| Level | Content |
|---|---|
| Title | `AUDITX GENERATION N` in blue. |
| Scope | One `Scanned:` line in dark gray naming what the run read. |
| Result | The `Result` section with every gate and the verdict. |
| Group | Totals per folder, project, kind, reason, pair or verdict. |
| File | Rows per file. |
| Item | Single lines, names, members or hits. |
| Reports | One `Report: <path>` line per written file. |

## Sections

A blank line comes before every section heading and before the report lines.
A heading is underlined with dashes of its own length.
A list section heading ends with ` (N)`, where N counts every row, shown or not.
A summary or ranking section heading carries no count.
A group table that splits a whole ends with a `Total` row.
A list section with nothing to list is left out, since its gate already reads 0.
A section cut short ends with `... and N more in the report.`
Each colour has one meaning, and nothing else wears it.

| Colour | Meaning | Where |
|---|---|---|
| Blue | Heading | The title line and every section heading. |
| Cyan | Column | Every table header row and the dashed rule right below it. |
| Dark gray | Frame | The scope line and every section heading's dashed underline. |
| Green | Pass | The `OK` status word and the `PASS` verdict line. |
| Red | Fail | The `FAIL` status word and the `FAIL` verdict line. |
| Yellow | Warning | The `WARN` status word, a warning line, a warning-band section heading, and the paging prompt. |
| Default | Content | Table rows, meanings, counts, paths and `Report:` lines. |

Colour marks a word or line only for its meaning, so a `Result` row colours its status word alone.

## Result

Every gate is one row of the `Result` table, so a done run shows every row at 0.
The table has the columns `Status`, `Count`, `Gate` and `Meaning`.
`Status` reads `OK` in green when the count is 0 and `FAIL` in red otherwise.
A warning gate carries a ceiling.
It reads `WARN` in yellow while its count sits above 0 and at or below the ceiling.
Above the ceiling it reads `FAIL`.
A passing verdict with a `WARN` row names each warning gate's section.
`Gate` is the label of the gate, and a gate that lists findings carries its finding section's label.
`Meaning` says in a few words what the gate counts.
A verdict line follows the table after a blank line.
It reads `PASS: all N gates at 0.` in green, or `FAIL: X of N gates above 0.` in red.
A failing verdict names each failing gate's finding section after `See`.
The audit exits 1 when any gate reads `FAIL` and 0 otherwise.
A number that gates nothing belongs in the scope line or a group section.

## Tables

Columns are split by two spaces and nothing starts indented.
A dashed rule sits under the header and matches each column width.
Numbers align right and text aligns left.
Every count carries thousands separators, while a `path:line` location never does.

## Plain text

A warning prints as a plain line and never through `Write-Warning`.
A failure the audit cannot judge throws and exits with an error.
`Check.ps1` reads only the `Status`, `Count` and `Gate` columns of the `Result` table, so they are the whole contract.
