# TAuditLineSetting.cs
Hash: `a3b4a105a2e57f78`

## `internal static class TAuditLineSetting`

The line-audit thresholds, ceilings and scope live here, hand-written and tracked, not in a generated sidecar.
`TAuditLineLimit` is the last line count that passes and `TAuditLineWarning` the last that passes silently.
`TAuditWidthLimit` maps an extension to the widest line it allows, and `TAuditWidthBand` sets the warning band under it.
`TAuditLineCeiling` counts how many hits of each kind may stand, and the ratchet holds it from rising.
Its kinds cap the over-long files and over-wide lines Cadroue still holds.
The ratchet lets each `Limit` fall and `TAuditLineEnforced` switch on, and fixes every other field.
AuditLines.ps1 reads its own tracked AuditLines.json and never writes this file.
The audits read no script configuration, and both copies change together as scripts/principles.md asks.
