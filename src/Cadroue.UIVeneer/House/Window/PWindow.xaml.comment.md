# PWindow.xaml.cs

## `public partial class PWindow : Window`

The main window is the concrete composition root for its WPF shell.
It wires the strip, rail, toolbar, console and deck to the supplied `QWindow`.
Partial peers isolate drop, shortcut, resize and native-window handling.

## `public PWindow(QWindow lWindowRoot)`

The window supplies `PStrip` with the one concrete Export factory.
That factory creates `PExport` and prepares it through `QExportSurface`.
It supplies the production encoder, verdict, warning, preset-group, row, preset and command implementations.
Concrete WPF choices remain at the shell root while the strip requests a prepared surface for each workspace's `QExport`.

The window also supplies `QLogoAnswer` with the concrete About, Options, Shortcuts, Log and Debug presentations.
`QLogoMenu` receives that passive packet, the toolbar contract and the shared Chrome state, then owns menu construction and selection routing.

Construction attaches the window dispatcher and console before notices.
It attaches the deck before startup creates tabs and restores size before tab startup.
Options apply before position restoration.
The selected tab is sent to `QWindow` before the relay starts.

## `private void PWindowNoticesAttach()`

The window translates `QWindow`, drop, shortcut, ask and warning notifications into control operations and presentations.
The shortcut route receives the same immutable logo answer used by pointer selection.
Close detaches the global ask and warning notices, selection, shortcut hook, drop and resize handlers, then closes every deck workspace.

## `private void PWindowTabAttach(QStripTab lTab, double pFlowHeight)`

Tab attachment applies workspace flow and viewer state before following the surface width.
Detachment stops that width subscription before resetting workspace commands.

## `private void PWindowExitHandle(object? sender, System.ComponentModel.CancelEventArgs eventArgs)`

A refused strip-close confirmation cancels the window close.
An accepted close saves normal and restore bounds before closing the window deportment and UI workspaces.

## PWindowDrop.cs, PWindowResize.cs and PWindowShortcut.cs

The peer adapters translate WPF drag, pointer, capture and native key state into their UIDeportment owners.
They apply returned handled, cursor, render and bounds state to WPF and expose explicit removal or detachment during close.

## PWindowDwm.cs

Native-window setup suppresses erase-background handling, requests rounded corners and caption color, and reapplies the executable icon after rendering.
A missing `HwndSource` skips erase-hook attachment.
Native attribute and icon-call return values are ignored.

## PWindow.xaml

Markup owns the borderless window grid and named toolbar, rail host, console and deck surfaces.
Runtime behavior and composition remain in code-behind and its peers.
