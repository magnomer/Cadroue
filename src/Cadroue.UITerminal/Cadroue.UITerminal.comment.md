# Cadroue.UITerminal.csproj
Hash: `d12cce80ef190c84`

The console surface, the second UI beside the WPF veneer.
It holds no source yet.

## `<DisableTransitiveProjectReferences>true</DisableTransitiveProjectReferences>`

UITerminal compiles against Demeanor alone, never Conduct or the rings below it.
A name past Demeanor fails the build, not only the border audit.

## `<ProjectReference Include="..\Cadroue.UIDemeanor\Cadroue.UIDemeanor.csproj" />`

Demeanor is the only reference, as Deportment is the veneer's only reference.
A UITerminal member only calls a function, and Demeanor drives it.
It targets the portable framework, and a Windows API fails its build through CA1416.
