# TInterfaceEngine.cs
Hash: `18d64291a9b16177`

## `internal static void TInterfaceEngineStart() =>`

Runs the module start of `Cadroue.Tests.Interface` before any Engine test runs.
That start redirects the production home to a fresh temporary folder.
An Engine test may reach a store before it touches the facade.
Without this start, such a test would write into the real application-data folder.
