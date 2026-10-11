# LDepotEngine.cs
Hash: `84cf67db01aba8c6`

## `public static class LDepotEngine`

The single ShellEngine owner of every depot folder the engine reads.
The engine never names the Infrastructure depot, so the host assigns each seam at startup.
The test relay assigns the same seams, so tests resolve the same folders.
Each seam starts returning an empty path, so an unattached run resolves its files against the working folder.

## `public static Func<string> LDepotAudioSeam { get; set; } = () => "";`

Names the folder where audio stage files live.

## `public static Func<string> LDepotMergeSeam { get; set; } = () => "";`

Names the folder where merge list files live.

## `public static Func<string> LDepotBridgeSeam { get; set; } = () => "";`

Names the folder where bridge pieces and their join lists live.

## `public static Func<string> LDepotPassSeam { get; set; } = () => "";`

Names the folder where two-pass encode logs live.

## `public static Func<string> LDepotRootSeam { get; set; } = () => "";`

Names the depot root that holds the relay plan files.
