# TInterfaceDeportment.cs
Hash: `599c55d2f0259491`

## `internal static partial class TInterface`

The inspector-section part of the test interface, with the loupe, strip, column, export, group, ask and program forwards.
The inspector, crop, list and funnel forwards live in their own classes, one area each.
Most forwards add no logic.

## `internal static void TInterfaceHomeStart()`

Runs the module start of `Cadroue.Tests.Interface` before any Windows test runs.
That start redirects the production home to a fresh temporary folder.
A Deportment test may reach a store before it touches the engine facade.
Without this start, such a test would write into the real application-data folder.
