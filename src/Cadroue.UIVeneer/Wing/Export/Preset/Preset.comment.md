# Preset

These surfaces are passive structure for the Export preset list.
Each is a constructor that loads its markup and nothing else.
`QExportPreset`, `QExportHeader` and `QExportRow` in Deportment fill every text, icon, brush and handler.

## PExportPreset and PExportRow

Both are resource dictionaries that hold one control template.
The driver loads the template, so the list stays an exact `ScrollViewer` and a row stays an exact `Border`.
A subclass would change those types, and tests assert them.
A template-loaded row has its own name scope.
The driver reaches its parts by child position and the name editor by resource key.

## PExportPresetGroup

The group header is a `Border` built per header.
The header driver sets its `Header` tag, not the markup.
The drag code counts headers by that tag.

## Stamp

Commit: `b84e5458de50`
- `PExportPreset.cs` `8d6803581fb434dd`
- `PExportPreset.xaml` `1194ddf3c93ebdf0`
- `PExportPresetGroup.cs` `2a2ee0f8ca451953`
- `PExportPresetGroup.xaml` `e6cd0cc548cb35a0`
- `PExportRow.cs` `2d6fd4cfa30e607c`
- `PExportRow.xaml` `15d48d8ce71c312b`
