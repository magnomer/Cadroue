# PSOptionsSystemMpv.cs
Hash: `ee1422da27cd63ef`

## `private UIElement PSSystemMpvBuild()`

Builds the local mpv install button and its open-folder button for the Playback page.
The install runs through LMpv, so the page holds no library logic.
After each install, QSOptions rechecks mpv so the engine choice is enabled only when mpv is usable.
