# TInterface.cs
Hash: `dca9903192b44ddc`

## `internal static partial class TInterface`

Test-side boundary for production operations.
Members relay operations, configure seams or compose setup calls.
The boundary includes test initialization rather than only one-operation relays.

## `internal static void TInterfaceHomeStart()`

Module initialization establishes a fresh temporary home before this test module's ordinary methods run.
The Engine and Windows test projects force it from their own module starts.
The `AppContext` seam redirects production home resolution and its default depot beneath that temporary folder.
Preference, binding, frame, renderer, scene and preset stores resolve beneath the redirected home.
The keyframe and media-end caches resolve there too, and the relay store beneath its `local` subfolder.
It reads only the inlined key constant, which lives in Core's `LDepotHome`.
The referenced Core holder contains constants only, so reading the key needs no production home lookup.
The home reads themselves live in `LDepot` and `LKeyframeCacheStore`, which it never names.
It names no Infrastructure member itself, and attaches every seam only after the redirect.
Naming one would load that assembly first, and its module start would read the real preference.
Its purpose is keeping test runs out of the user's real application-data folders.
Explicit paths and depot overrides remain possible, so initialization alone does not guarantee isolation for every test.

## `internal static void TInterfaceSeamAttach()`

Points the Application, Media and Core seams at the Infrastructure stores, as the host does.
It also binds the Infrastructure seams that read preferences, text, batch time, retention rules and the librarian load.
Tests therefore reach the stores through the same seams the program uses.
The preset selection seams point at the preset owner, as in the host.
It stays non-inlined, so the initializer never loads Infrastructure before the home redirect.

## `internal static void TInterfaceTraceAttach()`

Points the engine trace seams at the same Infrastructure targets the host assigns.
It stays a separate non-inlined method, so compiling the initializer never loads Infrastructure early.
A relay that captures the seams calls it again on teardown, so later tests trace normally.

## `internal static void TInterfaceStoreAttach()`

Points the engine depot, inventory, liveness, lease and checkup seams at the targets the host assigns.
It also gives the Infrastructure checkup the engine flaw scanner, as the host does.
Like the trace attach, it stays non-inlined, so the initializer never loads Infrastructure before the redirect.

## `internal static void TInterfaceProcessAttach()`

Points the engine process, tool, probe, keyframe and lane seams at the Media targets the host assigns.
The suspend, resume and live seams point at the Media child table, as in the host.
It stays non-inlined like the other attaches, so the initializer never loads Media before the redirect.

## `private static void TInterfaceHomeClear(string home)`

Process exit attempts to remove the temporary home so successful cleanup leaves no folder behind.
The depot index is released first to checkpoint its journal and clear SQLite connection pools.
A folder still locked at exit is left in place rather than failing the run.
