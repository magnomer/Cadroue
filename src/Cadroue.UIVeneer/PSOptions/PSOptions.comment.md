# PSOptions.cs
Hash: `1a1d5768f730cbe3`

## `internal static void PSOptionsShow(Window pOwner, Action? pApplyCallback)`

Opens the options window modally over its owner.
Pages live in sibling partial files and all seed their controls from one LSOptions draft preference.
The apply callback lets the caller react to saved settings without the window knowing the caller.

## `private PSOptions(Window pOwner, Action? pApplyCallback)`

Every page control is created here before any page is built.
The apply pass reads every control, so no control may depend on its page having been shown.
The mpv engine choice follows LSOptions, which rechecks mpv in the background and reports the result.

## `private UIElement PSOptionsRootBuild(UIElement pSheetContent)`

Each sheet carries its own Apply, OK and Cancel footer, so the buttons stay visible on every page.
Cancel only closes, so the draft is dropped and the stored preference stays untouched.

## `private void PSOptionsApply()`

Copies every control into the draft, then lets LSOptions save it as one preference.
The apply callback runs even when saving failed, so the program reflects what the user chose.
A failed save is reported and skips the restart notice.
The restart notice appears only when LSOptions reports a setting that needs a restart.

## `private string PSOptionsWorkspaceResolve(string psWorkspaceFolder)`

An occupied workspace folder is refused, and the box reverts to the folder that is kept.
The warning names the resolved depot root, so the user sees which folder already holds work.
