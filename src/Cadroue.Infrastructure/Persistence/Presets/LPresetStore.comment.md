# LPresetStore.cs

## `private static LPresetCatalog LPresetCatalogRead(string lPresetPath)`

The vault moves damaged storage aside as ".corrupt" before reporting it unreadable.
So a file that is gone afterwards was preserved and the catalogue may start fresh.
One that is still there could not be quarantined (locked or denied) and stays unreadable.
That blocks every later write so a temporarily unavailable catalogue is never replaced by a fresh one.
