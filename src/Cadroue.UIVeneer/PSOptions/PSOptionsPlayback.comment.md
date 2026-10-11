# PSOptionsPlayback.cs
Hash: `c1790d0e6f4a61f4`

## `private UIElement PSPlaybackBuild()`

Builds the Playback page with the preview engine, autoplay, volume, wheel and drag plates.
Choice rows keep their selection in the control and the apply pass copies it into the draft.

## `private UIElement PSPlaybackEngineBuild()`

The engine choice shares one plate with the local Flyleaf and mpv install buttons.
Each button label states its library's status, so the user sees what the chosen engine needs.
