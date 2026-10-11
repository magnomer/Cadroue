# Cadroue.Tests.Interface.csproj
Hash: `b37032248abb2795`

Builds the engine test facade: relays, fakes and the test workspace.

## Project references

References Core, Application, Infrastructure, Media, ShellEngine and Conduct.
It never references Deportment or the veneer.
The engine test project reaches production only through this facade.
The Windows test project reaches the Deportment through relays of its own.
Both see the internal relays, fakes and workspace here through `InternalsVisibleTo`.

## Tests

Tests here sit beside the relays and may call production directly.
`TInterfaceBoundary` scans every other test project except the convention project.
It fails on a direct production call outside an `Interface` folder.
