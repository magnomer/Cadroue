# LPresetStore.cs

## `public static bool LPresetCatalogCheck(string lPresetFilePath)`

A standalone export writes one record, but the catalogue holds a list of records.
Writing the export over the catalogue path would make the next load unreadable and lose every stored preset.
So the export writer refuses that path outright, before any file is touched.

## `private static LPresetCatalog LPresetCatalogRead(string lPresetPath)`

The vault moves damaged storage aside as ".corrupt" before reporting it unreadable.
So a file that is gone afterwards was preserved and the catalogue may start fresh.
One that is still there could not be quarantined (locked or denied) and stays unreadable.
That blocks every later write so a temporarily unavailable catalogue is never replaced by a fresh one.
