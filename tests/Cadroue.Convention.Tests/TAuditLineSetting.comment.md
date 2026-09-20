# TAuditLineSetting.cs

## `internal static class TAuditLineSetting`

Hand-written and tracked: the line-audit thresholds, ceilings and scope live here, not in a generated sidecar.
`TAuditLineLimit` is the last line count that passes and `TAuditLineWarning` the last that passes silently.
`TAuditWidthLimit` maps an extension to the widest line it allows, and `TAuditWidthBand` sets the warning band under it.
`TAuditLineCeiling` counts how many hits of each kind may stand, and the ratchet holds it from rising.
auditlines.ps1 reads its own gitignored auditlines.json and never writes this file.
Keep the two in agreement by hand, because the tests must depend on nothing untracked.
