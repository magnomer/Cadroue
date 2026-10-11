# TEncodeCommand.cs
Hash: `aef56e896a0f7bc8`

## `internal sealed partial class TEncodeCommand : IDisposable`

Test-side relay for production command construction and its depot configuration.
Each instance points the shared depot root at its own temporary folder.
Disposal restores the root that was in effect before, so later tests see the same effective depot.
