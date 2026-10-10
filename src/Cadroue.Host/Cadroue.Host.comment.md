# Cadroue.Host.csproj

The composition root that starts the veneer's application and wires the deportment's startup into it.
It builds the executable `Cadroue.exe`, so the installed program keeps its name.

## Project ring

It is the only project that may name every project.
It holds no behaviour, and the WPF entry point adds only what depends on its medium.
It takes the veneer's framework, runtime identifiers and icon, since it is now the executable.
It uses WPF because it starts the veneer's application on its own thread.
The Release publish is precompiled with ReadyToRun.
Precompiled code removes first-use JIT from launch and first clicks.

## Notices

`THIRD-PARTY-NOTICES.md` and `LICENSE` are copied beside the executable, outside the single file.

## `ExcludeFlyleafFromSingleFile`

Keeps every `Flyleaf` file out of the single-file bundle and copies it beside the executable.
