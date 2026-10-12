# PSEncoderLocation.cs
Hash: `758035c59d2ca6cd`

## `private UIElement PSLocationFieldBuild(TextBlock psLocationStatus)`

Builds the output location row with the mode toggle and a status line beside it.
The output plate creates the status line, and PSLocationModeUpdate rewrites it for each mode.

## `private void PSLocationModeUpdate()`

Each folder mode keeps its own folder text, so switching modes does not lose what was typed.
QSEncoder stores the text under the previous mode and returns the text saved for the new one.
The folder row hides for the source mode, and the browse button shows only for a custom location.
