# Toolbar

## PToolbar.xaml

The fixed toolbar structure exposes `PLogo` as the logo-menu contract element.
Markup carries no logo-menu event hook.

## PToolbar.xaml.cs

The toolbar stores the shared Chrome state but does not store, construct or forward the logo menu.
Its press and move routes update Chrome state.
`QLogoMenu` resolves that state on logo-button release.

## Stamp

Commit: `9a34a722aa84`
- `PToolbar.xaml` `a6f9adf84e0d53b0`
- `PToolbar.xaml.cs` `2d16eeb5e0462a6e`
