# TInterface.cs

## `internal static partial class TInterface`

Test-side boundary for production operations.
Members relay operations, configure seams or compose setup calls.
The boundary includes test initialization rather than only one-operation relays.

## `internal static void TInterfaceHomeStart()`

Module initialization establishes a fresh temporary home before this test module's ordinary methods run.
The `AppContext` seam redirects production home resolution and its default depot beneath that temporary folder.
Preference, binding, frame, renderer, scene and preset stores resolve beneath the redirected home.
The keyframe and media-end caches resolve there too, and the relay store beneath its `local` subfolder.
It reads only the inlined key constant, which lives in Core's `LDepotHome`.
The referenced Core holder contains constants only, so reading the key needs no production home lookup.
The home reads themselves live in `LDepot` and `LKeyframeCacheStore`, which it never names.
It names no infrastructure member.
Naming one would load that assembly first, and its module start would read the real preference.
Its purpose is keeping test runs out of the user's real application-data folders.
Explicit paths and depot overrides remain possible, so initialization alone does not guarantee isolation for every test.

## `private static void TInterfaceHomeClear(string home)`

Process exit attempts to remove the temporary home so successful cleanup leaves no folder behind.
The depot index is released first to checkpoint its journal and clear SQLite connection pools.
A folder still locked at exit is left in place rather than failing the run.
