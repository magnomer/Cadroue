# TAuditLineRow.cs

## `internal sealed record TAuditLineRow`

One source file after it is read, with the numbers the facts read.
`TAuditLinePath` is the repo-relative file.
`TAuditLineCount` is its line count.
`TAuditLineWidth` is the width limit of its extension, or the maximum integer when none applies.
`TAuditLineWide` lists each line inside the width band or above it, with its number and width.
