# LPresetSelection.cs

## `public sealed class LPresetSelection`

One preset never varies inside one window, and that includes an unsaved draft.
The working record for a preset name lives here, once.
Every tab showing that name is a view onto it.
A tab holds no export state of its own.
So an edit in one tab is the same edit in every other.
Work already commissioned is unaffected: an enqueued item carries its own encoding snapshot and never reads back through here.

## `public bool LPresetSelectionSave(string lPresetName)`

The working record is the draft itself, so a save hands storage exactly what every tab is looking at.
A rejected write puts the draft back under the name it came from.
A save under a different name returns that name to its stored values.
So a preset is never left dirty by a copy made out of it.

## `internal static void LPresetDraftSync(string lPresetName, LPresetRecord? lPresetStored, LPresetRecord? lPresetPrevious)`

The catalogue changed underneath the drafts.
A draft still holding the stored values is not an edit, so it follows the catalogue.
One the user has modified is kept.
