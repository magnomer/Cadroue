# PRosterCard.cs
Hash: `f39505e85bc4b0e7`

## `internal static class PRosterCard`

Builds the framed entries of a batch card from Deportment records.
Every brush, border and corner comes from the theme tables, keyed by values the records carry.

## `private static readonly IReadOnlyDictionary<string, Func<LRoster, LRosterEntry, ContextMenu, UIElement>>`

Picks the builder by entry kind, so the head, file and row entries share one frame builder.

## `public static Border PRosterEntryBuild(LRoster lRoster, LRosterEntry lEntry, ContextMenu pMenu) => new()`

Wraps the entry body in a frame shaped by its position in the card.
Neighbouring frames therefore join into one rounded card.

## `private static Border PRosterHeaderBuild(LRoster lRoster, LRosterCard lCard)`

Releasing the title selects the whole batch.
The collapsed state decides which corners are rounded.

## `private static Border PRosterToggleBuild(LRoster lRoster, LRosterCard lCard)`

Toggles collapse of the card.
Hover redraws the icon in the text color, and leaving restores the card's control color.

## `private static Border PRosterCloseBuild(LRoster lRoster, LRosterCard lCard)`

Removes the batch through the Deportment, which may ask first.
The glyph turns to the failure color on hover.

## `private static Border PRosterControlBuild(UIElement pContent, string pTooltip)`

A small square button that marks a mouse down handled.
The press therefore never bubbles to the card, its frame or the queue.
