# PSKeymap.cs
Hash: `69dac99d83d7555b`

## `internal static void PSKeymapShow(Window pOwner, Action? pApplyCallback)`

Opens the shortcuts window as a modal over its owner.
The apply callback runs after each Apply or OK, so the owner can reload its shortcut map.

## `private PSKeymap(Window pOwner, Action? pApplyCallback)`

One chord field is created per QSKeymap chord before the sheets are built.
Rows then find their field by command token, so field state lives in QSKeymap, not in the sheets.

## `private UIElement PSKeymapScopeBuild(string pScope, string pScopeTitle)`

The rows follow the command order LBinding gives for the scope, with the editing notice last.

## `private void PSKeymapDefaultApply() => lsKeymap.QSKeymapDefaultApply();`

Reset only fills the fields with the defaults.
Nothing is stored until Apply or OK, so Cancel still discards the reset.

## `private void PSKeymapApply()`

Stores every field through QSKeymap, which hands normalization to LBinding.
