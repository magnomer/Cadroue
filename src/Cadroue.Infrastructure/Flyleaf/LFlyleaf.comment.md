# LFlyleaf.cs
Hash: `a70593c99623bef1`

## `internal static void LFlyleafModuleStart()`

It runs as the module initializer, so the local Flyleaf resolver is attached on first use of this assembly.
The module start reads the preference file before any owner exists, so it applies defaults and normalize itself.
The store returns the state raw, and both calls are Core members, so no Application type is named.
The depot root is set first, because the local Flyleaf build lives under it.

## `private static bool LFlyleafLoadedCheck(string lAssemblyName)`

Flyleaf counts as active only when the loaded assembly came from the local build folder.
A NuGet copy loaded from the probe path does not count.

## `private static void LFlyleafShadowApply(bool lLocalActive)`

The NuGet FlyleafLib beside the program would win the probe over the local build.
So it is renamed aside while a local build is active, and renamed back once none is.

## `public static string? LFlyleafFolderRead()`

A record whose runtime folder fails validation but whose source checkout remains is rebuilt into a fresh runtime.
Any failure reads as no local build, so startup falls back to the NuGet copy.

## `private static bool LFlyleafFolderValidate(string? lAssemblyFolder)`

Only the runtime folder under the Flyleaf root qualifies, never a build output or reference folder.
