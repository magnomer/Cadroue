# PSEncoderCodec.cs
Hash: `428295c71eed55c6`

## `private void PSCodecProbeDefer() =>`

The startup encoder probe may still run when the dialog opens.
Its completion refills the video encoder list, unless the window has already closed.

## `private void PSVideoEncoderUpdate()`

Shows the unavailable notice when the chosen video encoder failed the last trial probe.
An encoder the probe never tested counts as available.

## `private void PSCodecContainerHandle()`

Refills the video encoders that suit the current container and passed the probe.
The current choice is kept when it still fits, otherwise the first item is selected.

## `private async Task PSCodecVerifyHandle(ComboBox pCombo, Button pButton, IProgress<double> pFeed)`

Runs a fresh trial encode over every video candidate and keeps only those that worked.
Only closing the window cancels the scan, so a cancelled scan returns without touching controls.
