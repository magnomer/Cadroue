# LUsher.cs
Hash: `6eea3d3da157d05e`

## `public static string LUsherProgramResolve(string lUsherName) =>`

Returns the full path of a file shipped beside the executable.
The UI asks here, so no Veneer or Deportment code combines paths itself.

## `public static string LUsherPathResolve(string lUsherPath)`

Returns the absolute form of a path, or the path unchanged when it is malformed.

## `public static string? LUsherPathOpen(string lUsherPath, string lUsherLabel, string? lUsherFallback = null)`

Reveals a file in Explorer, or opens a folder when the file is gone.
That folder is the fallback when given, else the file's own folder.
A fallback folder is created first, so the caller always lands somewhere.
A missing folder of the file itself opens nothing.
A failure is recorded under the caller's label and its error is returned.

## `public static string? LUsherFolderOpen(string lUsherFolder, string lUsherLabel)`

Opens an existing folder in Explorer and does nothing for a missing one.
A failure is recorded under the caller's label and its error is returned.

## `public static string? LUsherLinkOpen(string lUsherTarget, string lUsherLabel)`

Opens a file or link with its system handler.
A failure is recorded under the caller's label and its error is returned.

## `private static string LUsherFailureRecord(string lUsherLabel, string lUsherTarget, string lUsherError)`

Records a failed open as `{label} could not be opened`, with the target and the error.
The opener records it, so no caller writes its own trace line for the same failure.
The error comes back so the caller decides whether to show it.
