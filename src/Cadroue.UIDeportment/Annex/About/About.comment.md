# About

This folder holds the driver of the About window.
The window is passive, so its lifetime, placement and requests run here.
Conduct decides what the About means, and this driver only asks and paints.

## QSAbout.cs

`QSAbout` finds each named part of the window through the contract and fills it with localized text.
It takes the window, the dialog shell, the grabber, the warning and the placement actions as injected parts.

The declared body `PSAboutBody` is lifted out of the window.
The shared dialog shell can then frame it under the band.

### QSAboutShow

Opens the about window as a modal over its owner.
The texts, the credit rows and the requests are attached before the window shows.
The load handler, the grabber and the close handler follow in that order.

### QSAboutTapHandle

Quick repeated taps on the program mark toggle developer mode through `CSAbout`.
The developer line is updated only when a toggle actually happened.

### QSAboutErrorShow

Shows a failed open as a warning over the about window.
The usher already writes the trace record, so only the warning is shown here.
Nothing is shown when the open succeeded.

### QSAboutLoadedHandle

The window first sizes to its content.
It then freezes that height before the saved placement is restored.
The minimum height wins when the content is shorter.
The handler removes itself, so it runs once.

### QSAboutCloseHandle

Saves the placement under the About key before the grabber is detached.
The next opening therefore returns to the same spot.

## Stamp

Commit: `79b8ef3aa5dd`
- `QSAbout.cs` `7f1d3b6790c3a011`
