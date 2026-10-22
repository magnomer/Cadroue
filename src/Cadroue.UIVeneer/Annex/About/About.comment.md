# About

The XAML provides named WPF scaffolding for another layer to populate and drive.

`PSAbout` only initializes its component and applies the shared dialog look.
It stores no application state and registers no event handlers here.

## PSAbout

The window is fixed in width and sizes to its content in height.
It cannot be resized.

The close button sits in a bottom row outside the scroll viewer.
Scrolling therefore does not move it.

`PSAboutBody` is the dock panel that holds all content.
The driver lifts it out to frame it in the shared dialog shell.

The header names the mark, developer line, brand, version and project link.
The text blocks and the hyperlink start empty.

The credits part names the title, the gallery host, the license label and the document link.
The gallery starts empty.

`PSAboutCredit` is the keyed template for one credit row.
It names a label and a link, which the driver resolves inside each loaded row.

## Test

Test `TAboutSurface` resolves every name through the same contract the driver uses.
It resolves the credit row parts inside a loaded template.

## Stamp

Commit: `79b8ef3aa5dd`
- `PSAbout.xaml` `8e714e88eae448c6`
- `PSAbout.xaml.cs` `2d44d65eca711707`
