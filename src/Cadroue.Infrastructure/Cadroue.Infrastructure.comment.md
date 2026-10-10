# Cadroue.Infrastructure.csproj

Builds the storage, settings and depot layer.

## Project ring

References `Cadroue.Core`, `Cadroue.Application` and `Cadroue.Media`.

## Presets

Every file under the root `presets` folder is embedded under its relative path.
The shipped presets are read from the assembly, never from disk.

## `CA2255`

The analyzer warning on module initializers is silenced for this project alone.

## SQLite

`work.db` is backed by SQLite, whose native library ships per runtime identifier.
The shell sets `IncludeNativeLibrariesForSelfExtract` so single-file publish unpacks it.
The native bundle is referenced directly to lift it past a vulnerable transitive version.
`Microsoft.Data.Sqlite` 10.0.10 pins bundle 2.1.11, which carries GHSA-2m69-gcr7-jv3q.
Drop that reference once `Microsoft.Data.Sqlite` depends on a patched bundle by itself.
