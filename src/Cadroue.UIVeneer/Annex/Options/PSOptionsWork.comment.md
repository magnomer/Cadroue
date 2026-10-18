# PSOptionsWork.cs
Hash: `0bdd7931df225004`

## `private UIElement PSWorkBuild()`

Builds the Work page with the failure and retry plate and the process priority plate.
The retry limit row is enabled only while the retry box is checked.
The priority rows come from QSOptions, one per process family.

## `private static UIElement PSOptionsLevelBuild(QSOptionsLevel pLevel)`

A priority combo writes its level into the draft as soon as it changes.
The apply pass therefore does not read these combos back.
