# LSectionPalette.cs
Hash: `44ff0e872f80803b`

## `public static Func<string> LSectionPaletteSeam { get; set; } = () => LPreferenceState.LPreferencePaletteDefault;`

The active palette name, read through a seam because Infrastructure does not decide user settings.
The host binds it to the preference that names the palette.
Unattached, it gives the name an empty preference would hold.

## `public static string LSectionColorRead(int lColorIndex)`

Wraps any index, negative ones included, so every section gets a colour from the active palette.

## `public static void LSectionRegistryLoad()`

The default palette can never be hidden, so the registry always has one native entry.
A file with no valid colour is skipped.
A file named like an existing palette gets a numbered name, so every name stays unique.

## `public static string? LSectionHexNormalize(string lHex)`

Accepts `#RRGGBB` or `#AARRGGBB` and drops the alpha, since sections set their own transparency.
Anything else returns null, so the caller drops it.

## `public static LSectionImportResult LSectionPaletteImport(`

A name whose file name would start with a dot is refused, since dot files are reserved.
A source already in the palette folder is used where it lies, not copied.
A copy never overwrites, so it takes the next free numbered file name.
