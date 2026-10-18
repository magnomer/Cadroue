# PSAbout.cs
Hash: `9b04853eb4440ba7`

## `internal static void PSAboutShow(Window pOwner)`

Opens the about window as a modal over its owner.
The window uses the shared tabless dialog shell so it matches every other borderless window.

## `private void PSAboutLoadedHandle(object pSender, RoutedEventArgs pEvent)`

The window first sizes to its content, then freezes that height before the saved placement is restored.
The minimum height wins when the content is shorter.

## `private void PSAboutTapHandle(object pSender, MouseButtonEventArgs pEvent)`

Quick repeated taps on the logo toggle developer mode through QSAbout.
The developer line is updated only when a toggle actually happened.

## `private void PSAboutNoticeHandle(object pSender, RoutedEventArgs pEvent)`

Asks `QSAbout` to open the third-party notice shipped beside the executable.
A failure comes back through `QSAboutErrorShown`, which shows it as a warning.

## `private void PSAboutCloseHandle(object? pSender, EventArgs pEvent)`

Saves the placement under the About key so the next opening returns to the same spot.
