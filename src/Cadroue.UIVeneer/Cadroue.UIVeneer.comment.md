# Cadroue.UIVeneer.csproj

Builds the WPF veneer as a library that `Cadroue.Host` runs.

## Target

Targets Windows with WPF.
`App.xaml` is compiled as a page, since a library may not hold an application definition.

## Project ring

References only the deportment.
The lower projects still reach it through the deportment's own references.
It grants its internals to the `Cadroue` executable, which wires the startup.

## Resources

The icons under `PAsset` are compiled in as WPF resources.
They are read by a pack URI that names the `Cadroue.UIVeneer` assembly.
The localization catalogs are embedded under a `localization.` logical name.
The FFmpeg error catalogs are embedded under `localization.ffmpeg-error.`.
