# PSOptionsSystem.cs
Hash: `ad4144fa096c1e24`

## `private UIElement PSSystemBuild()`

Builds the System page with the FFmpeg, workspace, work record and file record plates.
The FFmpeg status line rechecks the folder on every edit, before anything is applied.
The workspace clear buttons follow the applied folder, never the folder being typed.
The default path notice shows only while the workspace box is empty.

## `private UIElement PSSystemFlyleafBuild()`

Builds the local Flyleaf install button and its open-folder button.
It sits on the Playback page beside the engine choice, not on this page.
The button label is the library status, so it is reread after every install attempt.

## `private static ProgressBar PSSystemProgressBuild()`

The bar stays collapsed except while an install runs.
Its template keeps only a rounded track and indicator, so it matches the plate look.

## `private static void PSSystemFolderRead(TextBox pPathBox, string pDialogTitle, string pFallback)`

Opens the folder dialog at the typed folder, or at the fallback while the box is empty.
A chosen folder only fills the box, and takes effect when the draft is applied.

## `private static void PSSystemFolderOpen(string pFolder, string pFallback, string pLabel) =>`

Opens the folder in Explorer, or the fallback while the folder is empty.
Each call site passes a label naming its folder, so a failed open says which one.
