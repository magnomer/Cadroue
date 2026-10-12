# PPlayerFlyleaf.cs
Hash: `c54e637ba2403575`

## `public PPlayerFlyleaf(FlyleafHost pHost, QPlayer lPlayerOwner, Config pConfig)`

Builds the Flyleaf player, registers its filters and hands the creation facts to `LRenderer`.
The creation is timed through `QPlayer` start and read calls, so the Veneer keeps no stamp.
The config values are read here, because only this layer sees FlyleafLib.

## `public static nint PPlayerHandleRead(System.Windows.Window? pWindow)`

A Flyleaf surface or overlay window may not exist yet.
A missing window yields a zero handle, so callers need no null check of their own.

## `public QPlayerSeam PPlayerSeamRead() => new(`

Hands the seam to `QPlayer`.
Flyleaf takes no filter strings and needs no paused refresh, so those slots stay `null`.

## `private void PPlayerOpen(string pSourcePath)`

A failed open throws with the message `LRenderer` resolves from the Flyleaf error.
The failure then reaches `QPlayer` like any other open fault.

## `private void PPlayerProcessorApply(string pReason)`

The preview filters are shader values, so the Flyleaf video processor is forced before they are set.
`LRenderer` records only a real switch, naming the processor that was active and the reason.

## `private LRendererFacts PPlayerFactsRead(VideoStream? pPlayerStream) => new(`

Reads the renderer and decoder facts from the live player as plain values.
`LRenderer` alone turns them into trace text and decides whether to record.
Each caller reads the video stream once and hands it in.
Flyleaf decodes on its own threads, so one read keeps a facts line on one stream.
The player sets its video decoder once at construction, so the caller reads it without a null check.

## `private void PPlayerDispose()`

The host lets go of the player before it is stopped and disposed.
A fault while stopping or disposing is swallowed, so closing a viewer never fails on a dying player.
