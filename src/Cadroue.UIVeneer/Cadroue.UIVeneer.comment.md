# Cadroue.UIVeneer.csproj

Builds the WPF veneer and the `Cadroue` executable, the startup project.

## Target

Targets Windows for x64 and arm64, with the program icon from `PAsset/PProgram`.

## Project ring

References every other source project, since the executable composes them all.

## Notices

`THIRD-PARTY-NOTICES.md` and `LICENSE` are copied beside the executable, outside the single file.

## Resources

The icons under `PAsset` are compiled in as WPF resources.
The localization catalogs are embedded under a `localization.` logical name.
The FFmpeg error catalogs are embedded under `localization.ffmpeg-error.`.

## `ExcludeFlyleafFromSingleFile`

Keeps every `Flyleaf` file out of the single-file bundle and copies it beside the executable.
