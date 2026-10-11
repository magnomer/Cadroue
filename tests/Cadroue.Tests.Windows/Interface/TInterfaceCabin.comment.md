# TInterfaceCabin.cs
Hash: `7a9a266b629dc802`

## `internal static partial class TInterface`

The cabin part of the test interface.
Tests reach the console, per-mode tab, action and compass deportments through these forwards.
The roster and funnel forwards live in their own classes, one area each.
Most forwards add no logic.
The seam forwards swap a static seam, and a null argument removes it again.

## `internal static void TFixScannerAttach(Action<string>? scanner)`

Swaps the diagnosis scan and the sidecar diagnosis read for a stub.
A null scanner removes both seams, so no test leaves a stub behind.
