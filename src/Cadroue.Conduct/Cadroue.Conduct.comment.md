# Cadroue.Conduct.csproj
Hash: `edb51d224f4dd76b`

Builds Conduct, the gate ring every driver calls to reach the logic behind the screen.
It holds no source yet, so the drivers still reach the engine directly.

## `<TargetFramework>net10.0</TargetFramework>`

Conduct names no window or control, so it builds for every platform.

## `<WarningsAsErrors>CA1416</WarningsAsErrors>`

A Windows-only call turns the build red, so the platform stays out of Conduct.

## `<ProjectReference Include="..\Cadroue.ShellEngine\Cadroue.ShellEngine.csproj" />`

Conduct reads the engine as its one neighbour.
Core and Application arrive through ShellEngine, never by a reference of their own.
