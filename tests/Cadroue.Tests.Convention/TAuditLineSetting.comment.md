# TAuditLineSetting.cs
Hash: `e2e62e247f812efa`

## `internal static class TAuditLineSetting`

The line-audit thresholds, ceilings and scope live here, hand-written and tracked, not in a generated sidecar.
`TAuditLineLimit` is the last line count that passes and `TAuditLineWarning` the last that passes silently.
`TAuditWidthLimit` maps an extension to the widest line it allows, and `TAuditWidthBand` sets the warning band under it.
`TAuditLineCeiling` counts how many hits of each kind may stand, and the ratchet holds it from rising.
The ratchet holds every other field here from changing without a commit.
AuditLines.ps1 reads its own tracked AuditLines.json and never writes this file.
The audits read no script configuration, and both copies change together as scripts/principles.md asks.
