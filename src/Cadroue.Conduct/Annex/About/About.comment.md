# About

This folder holds the About decisions.
It decides what the dialog credits, what it opens, and when the developer switch flips.
The window and its drivers live in other layers and only ask.

## CSAbout.cs

`CSAbout` credits five projects in a fixed order.
They are FFmpeg, FlyleafLib, MPV, Phosphor Icons and SharpVectors.
The project address is `CSAboutProjectUrl`.

The notice document is `THIRD-PARTY-NOTICES.md`, opened through the usher as a program.
A link is opened through the usher as a link.
Each open returns a failure text or nothing, and the caller decides how to show it.
The notice open also returns the resolved path, so the caller can name it.

The developer switch counts taps on the program mark.
A tap within 1.5 seconds of the last one adds to the count.
A later tap restarts the count at one.
The tenth tap resets the count and flips the developer preference.
`CSAboutTapChange` returns true only when the preference flipped.

## Stamp

Commit: `79b8ef3aa5dd`
- `CSAbout.cs` `4357276fbe107a20`
