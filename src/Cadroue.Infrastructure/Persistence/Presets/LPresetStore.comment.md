# LPresetStore.cs
Hash: `aaeb29a701b5c219`

## `public static IReadOnlyList<LPresetGroup> LPresetNativeLoad()`

Reads the shipped presets embedded under `presets/`, grouped by their first folder.
The folder overload reads the same layout from disk, so a tree can be checked without embedding it.

## `private static IReadOnlyList<LPresetGroup> LPresetGroupsLoad(`

A file directly at the root names no group, so it is skipped.
An invalid shipped preset throws, since a broken native catalogue is a build fault.
Groups sort by name and presets by path, so the order never depends on file enumeration.
Embedded records come back as read, and the preset owner normalizes them after its seam returns.

## `public static LPresetCatalog LPresetLoad()`

A latch timeout or an I/O failure reports the catalogue unreadable, never missing.

## `public static bool LPresetSave(Func<LPresetCatalog, IReadOnlyList<LPresetDocument>?> lPresetResolve)`

The read and the write share one latch, so no other process saves in between.
The resolver sees the catalogue just read and returns null to refuse the write.

## `public static bool LPresetCatalogCheck(string lPresetFilePath)`

A standalone export writes one record, but the catalogue holds a list of records.
Writing the export over the catalogue path would make the next load unreadable and lose every stored preset.
So the export writer refuses that path outright, before any file is touched.

## `private static LPresetCatalog LPresetCatalogRead(string lPresetPath)`

The vault moves damaged storage aside as ".corrupt" before reporting it unreadable.
So a file that is gone afterwards was preserved and the catalogue may start fresh.
One that is still there could not be quarantined (locked or denied) and stays unreadable.
That unreadable outcome lets the save resolver refuse replacement of a temporarily unavailable catalogue.
A loaded list drops null entries but is otherwise raw, since normalizing is the preset owner's job.

## `public static LPresetDocument? LPresetFileLoad(string lPresetFilePath)`

The preset import command and the native folder read call this directly, with no owner seam in between.
So it applies the document's own Core normalize, and an imported null field gets its default.
