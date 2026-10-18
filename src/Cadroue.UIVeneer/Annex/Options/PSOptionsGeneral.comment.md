# PSOptionsGeneral.cs
Hash: `c4a2f33e59f6a1e2`

## `private UIElement PSGeneralBuild()`

Builds the General page with the startup, confirmation, relay, layout and language plates.
Controls are created once in the constructor and read back when the draft is applied.

## `private UIElement PSOptionsStartupBuild()`

The tab picker shows only while the startup mode is the default tab.
Its visibility update is stored so the startup mode toggle can rerun it on each change.

## `private static LLocalizationChoice[] PSOptionsLanguagesRead()`

Each language is listed under its own name, read from that language's catalog.
