# LDepotHome.cs
Hash: `3d9929cab86f139e`

## `public static class LDepotHome`

Core holds only the constants that name Cadroue's per-user home.
It reads no environment, no `AppContext` and no path, so Core stays pure.
Each impure spoke applies these constants once in its own home read.
Infrastructure reads them in `LDepot`, and Media reads them in `LKeyframeCacheStore`.

## `public const string LDepotHomeKey`

The `AppContext` data key that redirects every per-user folder Cadroue writes.
It lives in Core so every project can honour it without reaching the depot.
The test module sets it during module initialization before its ordinary methods run.
It is a constant, so the test reads it without loading any production assembly.

## `public const string LDepotHomeFolder`

The folder name under `%AppData%` and `%LocalAppData%` when the key is unset.
Both spoke reads join it to the same system folder, so their user paths agree.

## `public const string LDepotHomeLocal`

The subfolder of a redirected home that stands in for the local home.
Only the local read in `LDepot` uses it.
