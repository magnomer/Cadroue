# TEditTabPlan.cs
Hash: `c4d6f936c819731c`

## `public sealed class TEditTabPlan`

Checks the Edit tab deportment plan reads, sidecar saves and text formats without any control.
Each tab test builds the tab over a real inspector, viewer and docket.
The Preset collection keeps it from running beside tests that share the preset store.

## `public void CarriedRead_NothingPersistent_IsNull()`

Only persistent settings travel to the next opened file.
Active but non persistent edits carry nothing.

## `public void CarriedRead_CropPersistentOnly_CarriesCropNotVideo()`

A persistent crop carries its edges and ratio lock.
Video and skip settings that are not persistent stay behind.

## `public void CarriedRead_VideoPersistent_CarriesOnlyPersistentKinds()`

The carried video plan holds only the kinds marked persistent.
Crop stays out when its own persistent flag is off.

## `public void StateSave_SourceLoaded_WritesRecordThroughLibrarian()`

The saved record goes to the librarian under the loaded source path.
It carries crop edges, rotation, flips and the crop active flag.

## `public void StateSave_NoSourceOrEmptyPlan_WritesNothing()`

No sidecar is written without a source.
No sidecar is written for a plan that holds no edits.

## `public void PersistentSave_CropPersistent_WritesEveryUnlockedPath()`

A persistent setting is saved to every unlocked docket path, in docket order.

## `public void Formats_DescribeCropAndVideo()`

These strings feed trace lines.
Missing and inactive values read as plain words so a log line never prints a blank.
