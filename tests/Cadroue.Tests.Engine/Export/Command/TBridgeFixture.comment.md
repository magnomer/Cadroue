# TBridgeFixture.cs
Hash: `e74936b83c1f3017`

## `internal sealed class TBridgeFixture : IDisposable`

Owns the temporary folder for one bridged-resilience test and synthesizes the awkward source media it needs.
That is delayed audio tracks, reordered B-frame GOPs, non-zero timelines.
Creation only, so it asserts nothing and measures nothing.
