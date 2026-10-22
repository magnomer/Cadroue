# Sensor

This folder holds the detector sections of the inspector.
The Conduct detector gate owns every state and decision.
These classes keep the Sensor names as a facade over the Conduct detector gate and drive the passive surface.

## QSensor.cs

The class is the facade and the passive surface driver for the medium-free detector gate.
It builds one body per detector kind from its plan.
It fills the preset combo from the gate's choice rows and paints the gate's state.
Every user act becomes one gate call, and nothing is decided here.
An attach flag is stored on the body resources.
Repainting clears the flag first and restores it in a finally block.
So the paint cannot echo back as a user act.
Detach removes every handler and clears the flag, so a detached body ignores late events.
Test `Facade_AndInspectorGateShareState_AndNoticeOnce` pins that the facade and the inspector gate share state and notify once.
Test `Repaint_DoesNotRequestSweep` pins that a repaint requests no sweep.

## QSensorName.cs

The class is a facade for the fixed processing and inspector names of the detector gate.
It only forwards to the Conduct plan.
Test `Plans_ExcludeBlank_AndKeepFixedNames` pins the names it forwards.

## QSensorPlan.cs

A plan projects a Conduct plan into localized wording, rows and groups.
It also finds the parts of a surface by name and fills them.
The threshold, window and minimum rows have fixed indices.
A row the Conduct plan lacks is collapsed.
Test `Plans_CoverEveryKindButBlank_WithRowsAndGroups` pins the coverage.

## QSensorPreset.cs

The preset wrapper turns the gate's choice rows into the combo's rows and selected index.
Test `Choice_IndexFollowsMatch_SelectAppliesPreset` pins the index and the apply.

## QSensorSweep.cs

The sweep driver turns the run and persistent acts into requests and paints the Conduct-owned state.

## Stamp

Commit: `b84e5458de50`
- `QSensor.cs` `d568dc8e42772f22`
- `QSensorName.cs` `85e2d93d524dab1a`
- `QSensorPlan.cs` `52ccd0c607dfd73c`
- `QSensorPreset.cs` `9993d818b02e1839`
- `QSensorSweep.cs` `5d067016f5fd0d92`
