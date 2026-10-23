# Clinic

## `PClinic`

`PClinic` owns the full clinic panel, minimized strip and salvage surface while `QClinic` supplies state and mutations.
Construction builds controls, attaches control and adapter events, then performs the initial paint.

Apply, persistent and diagnosis controls forward user intent to `QClinic`.
Clinic changes repaint the panel.
Minimize changes swap full and strip bodies and raise the presentation event.

Unknown steps show the empty notice and disable plan switches.
Salvage replaces the ordinary apply, persistent and diagnosis controls with salvage controls.

## `PClinicSalvage`

The salvage surface builds mode and basis choices.
It delegates their event wiring and repainting to `QClinicSalvage`.

## Stamp

Commit: `9a34a722aa84`
- `PClinic.cs` `e6580a3ad364cc75`
- `PClinicSalvage.cs` `bdb9ff1a45d1403d`
