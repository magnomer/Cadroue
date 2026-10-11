# LCheckupEngine.cs
Hash: `e3078fee8b88a410`

## `public static class LCheckupEngine`

The single ShellEngine owner of the cached Fix dossiers a job reuses.
The engine never names the Infrastructure checkup, so the host assigns each seam at startup.
The test relay assigns the same seams, so tests reach the same cache.

## `public static Func<string, IReadOnlyList<LDossier>?> LCheckupReadSeam { get; set; } = _ => null;`

Reads the cached dossiers of one source path, or null when none are kept.
Unattached, it reads null, so a job always scans.

## `public static Func<string, IReadOnlyList<LDossier>, Task> LCheckupSaveSeam { get; set; } =`

Stores the freshly scanned dossiers of one source path.
Unattached, it completes at once, so a run without a host keeps no cache.
