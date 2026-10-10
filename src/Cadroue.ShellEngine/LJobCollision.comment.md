# LJobCollision.cs

## `private async Task<string> LJobStreamValidate()`

Copy streams and Smart video retain the source codec.
The chosen container may not support that codec.
The encoder lists in Settings are filtered by container but are not consulted while Copy is selected.
For non-merge jobs, a readable probe rejects incompatible copied streams before encoding.
An unreadable probe leaves the pairing unchecked.
A fresh compatibility probe receives the runner's priority override, keeping preflight measurement under the same runner configuration.

## Inline notes

### `if (LJobClaim(pTarget))`

Claim the intended name atomically.
Success means it was free and is now ours.
Another caller using the same exclusive-create protocol cannot reserve that name.

### `if (string.Equals(pOutput.LEncodingCollision, "Rename output", StringComparison.Ordinal))`

Reservation failed, whether from an existing file, denied access or another I/O error.
Rename-output policy seeks another reservable name.

### `File.Move(pTarget, pFreePath, true);`

`pFreePath` is our own reservation placeholder, so overwriting it with the existing file is intended.
A genuine failure (locked/denied) must abort.
This protects the existing file from the encode that follows.

### `LJobClaim(pTarget);`

The existing file has moved aside.
Reclaim is attempted, but its result is ignored, so another instance can win the newly free target.
