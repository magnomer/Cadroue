# LJobCollision.cs

## `private string LJobStreamValidate()`

A stream set to Copy keeps the source codec, which the chosen container may not be able to store.
The encoder lists in Settings are filtered by container but are not consulted while Copy is selected.
So the pairing is caught here instead of surfacing as an FFmpeg muxing error part-way through the run.

## Inline notes

### `if (LJobClaim(pTarget))`

Claim the intended name atomically.
Success means it was free and is now ours.
So no second instance can pick the same "free" name and clobber this output.

### `if (string.Equals(pOutput.LEncodingCollision, "Rename output", StringComparison.Ordinal))`

The name is taken — a pre-existing file or another instance.
Apply the policy.

### `File.Move(pTarget, pFreePath, true)`

`pFreePath` is our own reservation placeholder, so overwriting it with the existing file is intended.
A genuine failure (locked/denied) must abort so the pre-existing file is never destroyed by the encode that follows.

### `LJobClaim(pTarget)`

The existing file has moved aside.
Reclaim the now-free target so no other instance grabs it before the encode writes.
