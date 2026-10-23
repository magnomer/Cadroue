# Porch

## QLogoAnswer.cs

`QLogoAnswer` is the immutable packet of concrete GUI presentations supplied by the Veneer composition root.
It holds callbacks only and owns no selection, lifecycle or application rule.

## QLogoMenu.cs

`QLogoMenu` owns logo-menu pointer release, runtime WPF construction, localization, icon adaptation and action routing.
It pulls `PLogo` from the toolbar contract, preserves the accepted action order, and delegates each selected presentation through `QLogoAnswer`.

`TLogoMenu.Build_PresentsAcceptedOrderTextIconsAndDebugVisibility` pins the order, text, icons, debug visibility, callback routing and same-action window close.

## Stamp

Commit: `9a34a722aa84`
- `QLogoAnswer.cs` `412614e9e218ea42`
- `QLogoMenu.cs` `e3e6694e60e42de6`
