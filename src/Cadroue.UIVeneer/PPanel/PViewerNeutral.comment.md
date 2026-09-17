# PViewerNeutral.cs

## `public async void PViewerEstimateRead(LWhitebalanceMethod pMethod, Action<LNeutralWheel> pEstimate)`

Decode the current frame and report where the given automatic method's neutral point falls, for the inspector's display-only colour-wheel estimate.
The export correction itself is computed later by ffmpeg's colorcorrect.

## `public async void PViewerFrameRead(Action<LMediaFrame?> pFrameReady)`

Decode the current frame and hand the raw RGBA pixels back, for the inspector's curve histogram guide.
Reuses the eyedropper's decode seam.
A null result means no media, no video, or a failed decode.

## Inline notes

### `pViewerNeutralTarget = pNeutralTarget`

Already armed: switch the sampler in place without re-pausing.

### `if (!pViewerPoint.LNeutralPointInside)`

Letterbox or no displayed pixel under the cursor: stay armed, no result.

### `PViewerNeutralReset()`

A valid click ends the tool immediately, and the decode runs in the background.

### `Size pViewerRotated = PCropDisplayRead()`

The Crop box is an overlay only.
No preview engine crops the frame, so the player always renders the whole rotated source under the cursor.
