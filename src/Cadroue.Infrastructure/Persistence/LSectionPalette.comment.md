# LSectionPalette.cs
Hash: `44ff0e872f80803b`

## `public static Func<string> LSectionPaletteSeam { get; set; } = () => LPreferenceState.LPreferencePaletteDefault;`

The active palette name, read through a seam because Infrastructure does not decide user settings.
The host binds it to the preference that names the palette.
Unattached, it gives the name an empty preference would hold.

## `public static string LSectionColorRead(int lColorIndex)`

Wraps any index, negative ones included, so every section gets a colour from the active palette.

## `public static void LSectionRegistryLoad()`

The overload with a folder loads from that folder, and the plain one loads from the depot palette folder.
The default palette can never be hidden, so the registry always has one native entry.
A file with no valid colour is skipped.
A file named like an existing palette gets a numbered name, so every name stays unique.

## `public static string? LSectionHexNormalize(string lHex)`

Accepts `#RRGGBB` or `#AARRGGBB` and drops the alpha, since sections set their own transparency.
Anything else returns null, so the caller drops it.

## `public static LSectionImportResult LSectionPaletteImport(`

The import asks the store to copy the file.
The registry reloads from the same folder only when the result is a copy.
The loaded name is looked up from the target path, so the caller selects the name the registry now holds.
Any other result leaves the registry unchanged and gives no name.
The store reports an unreadable palette as invalid.
A name whose file name would start with a dot is refused as reserved, since dot files are reserved.
A source already in the palette folder is used where it lies, not copied.
A copy never overwrites, so it takes the next free numbered file name.
