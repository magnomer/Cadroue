# TSplitTabLayout.cs
Hash: `fec8d2bf6e2dbc1e`

## `public sealed class TSplitTabLayout`

Checks the Split tab deportment layout and plan handling without any control.
The tab tests build it over a real inspector and a processing list holding one row per detector.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void Rows_OneStepPerKind_KeyedBySensorName()`

The processing rows must cover every detector kind exactly once.
Row keys and detector kinds must resolve back to each other.

## `public void LayoutRead_CarriesEveryDetectorAndPersistent()`

The layout holds all detectors, enabled or not, together with the persistent switch.
The blank detector keeps its colour type and hue.

## `public void LayoutApply_RoundTripsIntoSecondTab()`

A layout read from one tab and applied to another reproduces steps, preset tokens and the mode switches.
Processing activity follows the enabled detectors.
Plan saving is not left suspended after the apply.

## `public void LayoutApply_Null_MinimizesProcessingAndInspector()`

A tab restored without a saved layout starts with both panels minimized.

## `public void SensorChange_UpdatesProcessingActive()`

Enabling a detector in the inspector must show its row as active.
