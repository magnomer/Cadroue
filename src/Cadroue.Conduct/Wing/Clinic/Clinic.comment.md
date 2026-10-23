# Clinic

## `CClinic`

`CClinic` owns one Fix tab's shown source, selected step, minimized state and save-suspension depth.
It delegates plan storage to `CClinicPlan` and result storage to `CClinicCheckup`.

A selected step is known only when the plan contains its name.
Salvage is recognized by the literal `Salvage` step.
Unknown steps therefore expose no repair kind, result or persistence choice.

User plan changes raise both repaint and plan-save events only when stored state changes.
Accepted plans suspend saving in a `try`/`finally`, repaint once, then always resume saving.

Results and progress repaint only when they belong to the shown source.
Source matching ignores case.
Removing results repaints when the shown source had stored results.

## `CClinicPlan`

The plan preserves the engine's original flaw-kind order while storing repair and persistence choices by ordinal kind name.
Setters report whether state changed so `CClinic` can suppress redundant notifications.

Plan acceptance normalizes fixed salvage basis off when no repair step is selected.
Changed repair and salvage choices apply the same normalization.
Applying a plan emits every kind in engine order followed by the salvage choices.

## `CClinicCheckup`

Checkup storage groups results by case-insensitive source and ordinal kind.
A scanning result resets that source's progress to zero.
Any non-scanning result removes its progress.
Progress writes clamp values to the inclusive range zero through one, and removing a source clears both results and progress.

## Stamp

Commit: `9a34a722aa84`
- `CClinic.cs` `8b72a9c087dbf0b6`
- `CClinicCheckup.cs` `1dbdda0450352a69`
- `CClinicPlan.cs` `3aa2638599f2cdf7`
