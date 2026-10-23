# Clinic

## `QClinic`

`QClinic` adapts a `CClinic` gate for presentation.
Changed and minimize subscriptions pass directly through to the gate, while state and mutations remain gate-owned.

Diagnosis is the exception: `QClinicDiagnosisRun` raises the adapter's own request event.
The Fix tab owns the subscriber that turns that request into a checkup and removes it during close.

Result wording is localized in the fixed order required by `CClinicResultFormat`.
Missing simple or technical keys produce empty text.
A null gate fails construction through `ArgumentNullException`.

## `QClinicSalvage`

The salvage adapter attaches the presentation controls directly to gate setters and paints all controls from one `CClinicSalvageRead` answer.
The fixed-basis control is enabled only when the gate reports the plan fixable.

## Stamp

Commit: `9a34a722aa84`
- `QClinic.cs` `77a2219a1e478e84`
- `QClinicSalvage.cs` `3fa6ef166d02d582`
