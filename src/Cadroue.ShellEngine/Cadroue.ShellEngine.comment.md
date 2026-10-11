# Cadroue.ShellEngine.csproj
Hash: `1bed706a5a8c0a1d`

Builds the engine that runs FFmpeg jobs, sweeps and diagnoses.

## Project ring

References application only.
Media and infrastructure reach the engine only through seams assigned from outside it, mostly by the host.
Core arrives through Application, never by a reference of its own.

## Autopsy spine

`Autopsy/ffmpeg-error-spine.json` is embedded as `autopsy.ffmpeg-error-spine.json`.
`LAutopsySpine` reads it from the assembly by that name.
