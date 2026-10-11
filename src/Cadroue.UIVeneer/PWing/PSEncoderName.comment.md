# PSEncoderName.cs
Hash: `70dc5b0531cd9b60`

## `private UIElement PSNameRowBuild()`

Builds the elements row of chips that feed the output name box.
The name box sits in its own row just above this one.
Every chip edits the one token text the box carries back into the preset on apply.

## `private UIElement PSNameTokenBuild(string pLabel, string pToken)`

A chip inserts its token on a click and starts a drag once the pointer moves four pixels.
The drag flag stops the release that ends a drag from also inserting the token.

## `private UIElement PSNameOperatorBuild(string pWord, string pKind)`

An operator chip carries a count box, so it inserts or drags a token like `{Backspace:2}`.
A press inside the count box arms no drag and inserts nothing, so the count stays editable.
The token is formatted at release or drag time, so it always uses the current count.

## `private static int PSNameCountRead(string pText)`

An empty, zero or unreadable count falls back to one, so an operator never removes nothing.
