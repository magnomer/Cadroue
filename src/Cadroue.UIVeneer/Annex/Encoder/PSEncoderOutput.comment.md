# PSEncoderOutput.cs
Hash: `8b38a495c0224ab4`

## `private UIElement PSOutputPlateBuild()`

Builds the output plate with the name, location, container, extension and collision rows.
The location status line is created here and handed to the location field.
The suffix row and the location rows start in the state the loaded preset implies.

## `private void PSOutputSuffixNormalize() =>`

A suffix left blank is cleared in the draft, which brings back the default suffix for that policy.

## `private void PSOutputSuffixUpdate()`

The suffix row shows only for the two rename policies, each with its own label.
Each rename policy keeps its own suffix, so switching policies swaps the shown text.

## `private static LLocalizationChoice[] PSOutputExtensionRead(string pContainer)`

A container with no extension list offers a single choice that keeps the source extension.

## `private void PSOutputExtensionUpdate()`

The extension already chosen survives a container change when the new list still offers it.

## `private void PSOutputContainerHandle()`

A container change narrows the extension, video encoder and audio encoder choices together.
