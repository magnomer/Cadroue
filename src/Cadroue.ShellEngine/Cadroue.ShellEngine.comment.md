# Cadroue.ShellEngine.csproj

Builds the engine that runs FFmpeg jobs, sweeps and diagnoses.

## Project ring

References every lower ring: core, application, media and infrastructure.

## Autopsy spine

`Autopsy/ffmpeg-error-spine.json` is embedded as `autopsy.ffmpeg-error-spine.json`.
`LAutopsySpine` reads it from the assembly by that name.
