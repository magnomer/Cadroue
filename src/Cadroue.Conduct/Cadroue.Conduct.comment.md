# Cadroue.Conduct.csproj
Hash: `edb51d224f4dd76b`

Builds Conduct, the gate ring every driver calls to reach the logic behind the screen.
The drivers reach the engine through its gates, beginning with the Usher area.

## `<ImplicitUsings>disable</ImplicitUsings>`

Every namespace a file reads is written in that file.
The pure-ring walker then sees each frame namespace Conduct uses.

## `<InternalsVisibleTo Include="Cadroue.Tests.Conduct" />`

The gate tests build the root with a fake port through its internal constructor.

## `<InternalsVisibleTo Include="Cadroue" />`

Only Host, the `Cadroue` assembly, builds the root, so its constructor stays internal.

## `<TargetFramework>net10.0</TargetFramework>`

Conduct names no window or control, so it builds for every platform.

## `<WarningsAsErrors>CA1416</WarningsAsErrors>`

A Windows-only call turns the build red, so the platform stays out of Conduct.

## `<ProjectReference Include="..\Cadroue.ShellEngine\Cadroue.ShellEngine.csproj" />`

Conduct reads the engine as its one neighbour.
Core and Application arrive through ShellEngine, never by a reference of their own.
