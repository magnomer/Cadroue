# TAuditGateSetting.cs

## `internal static class TAuditGateSetting`

Hand-written and tracked: the Veneer/Deportment gate rules live here (C-5, U-VD-4, U-VD-5).
Forbidden entries are regular expressions matched against each line after literals and comments are removed.
Tolerated entries are the only spellings of a forbidden family a side may use.
A scoped entry grants one spelling in one file.
The scalar rule lists the scalar types, the transient suffixes, the draw-cache fields, and the skipped root file.
There is no baseline: any other hit fails the run.
