# TCompassButtons.cs
Hash: `7a37dabf2648ce92`

## `public sealed class TCompassButtons`

Checks the compass deportment button table, key dispatch and volume math without any control.
The veneer builds one button per record, so the table order here is the order on screen.

## `public void GroupsRead_HidesSectionGroups_UnlessShown()`

Section and keyframe groups appear only on tabs that show sections.
The read also reports which groups are the section ones.

## `public void PlayRead_SwapsFaceByPlaying()`

The play button keeps one key but changes icon and accent with the playing state.

## `public void Run_PlayKey_TogglesByViewer_OtherKeysReachFlow()`

The play key starts or pauses according to what the viewer reports.
The waveform toggle is checked here too and flips its flag on each call.

## `public void VolumeSet_ClampsAndSkipsEqual()`

A volume equal to the current one is not applied again.
Others are clamped to the range before being applied.

## `public void VolumeFormat_FillAndSeparators_ArePure()`

The volume text, track fill and separator opacities depend only on their arguments.
