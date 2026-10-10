# LPresetSelection.cs

## `public sealed class LPresetSelection`

One preset never varies inside one window, and that includes an unsaved draft.
The working record for a preset name lives here, once.
Every tab showing that name is a view onto it.
A tab holds no export state of its own.
So an edit in one tab is the same edit in every other.
Work already commissioned is unaffected.
An enqueued item carries its own encoding snapshot and never reads back through here.
The drafts are shared by every instance on every thread.
Dictionary access holds one private lock so shared draft entries cannot race during lookup or replacement.
The lock does not protect later mutations of returned draft records.
Direct change notices and the save seam run after the dictionary lock is released.

## `public bool LPresetSelectionSave(string lPresetName)`

The working record is the draft itself, so a save hands storage exactly what every tab is looking at.
A rejected write puts the draft back under the name it came from.
A save under a different name restores the original name's stored values when storage still contains that preset.
So copying a stored preset does not leave its original draft dirty.

## `internal static void LPresetDraftSync(string lPresetName, LPresetDocument? lPresetStored, LPresetDocument? lPresetPrevious)`

The catalogue changed underneath the drafts.
A draft still holding the stored values is not an edit, so it follows the catalogue.
While the catalogue still contains the preset, a user-modified draft is kept.
