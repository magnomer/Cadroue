# TAuditLine.cs

## `public sealed class TAuditLine`

Reports files that reach the line limit as an advisory.
The limit, roots, extensions, and excluded directories come from `TAuditLineSetting`, which `auditlines.ps1` writes.

## `public void AuditLine_OversizeFile_ReportsAsAdvisory()`

Counts the lines of every source in the line scope.
A file at the limit or above is printed with its count, largest first.
Nothing fails: the size of a file is a signal to split a responsibility, not a defect.
