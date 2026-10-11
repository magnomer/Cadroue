# LSidecarStore.cs
Hash: `8eaa927bb4cba7ad`

## `public static string LSidecarFolderResolve() =>`

Returns the record folder inside the current workspace, whether or not records are kept there.
The options page names it before the setting applies, and the apply step stores the same path.

## `public static string LSidecarPathRead(string lSidecarSourcePath)`

Returns where the `.cad` of a media file lives.
With the record folder active, the file is named by a hash of the full media path.
Otherwise it sits beside the media as `name.ext.cad`.
An older `name.cad` beside the media is moved to the new name on first read.

## `private static string LSidecarKeyCreate(string lSidecarSourcePath)`

The path is upper-cased before hashing, since Windows paths ignore case.

## `private static void LSidecarLegacyMove(string lSidecarFullPath, string lSidecarPreciousPath)`

Moves an old `name.cad` and its cache to the `name.ext.cad` form.
The move happens only when the old record names this very media file.
Two files sharing a stem would otherwise steal each other's record.
Any failure leaves the old file where it is.

## `public static int LSidecarFolderClear()`

Deletes every record and cache in the record folder.
The count is of `.cad` files only, since a cache is no record of its own.
Files that cannot be deleted are skipped.

## `public static LSidecar? LSidecarLoad(LKeyframeSourceIdentity lSidecarIdentity)`

A record whose source identity no longer matches the media is treated as absent.

## `public static LSidecarCoreDocument? LSidecarCoreRead(string lSidecarSourcePath)`

Reads the matching core record with queued unsaved edits laid over it.
A caller therefore sees an edit before its deferred save reaches the disk.

## `internal static LSidecarCoreResult LSidecarCoreResolve(string lSidecarSourcePath, string lSidecarPreciousPath)`

Tells apart a missing, unreadable, malformed, foreign and matching record.
Save decides by this kind whether it may overwrite the file.

## `private static bool LSidecarCoreSave(string lSidecarSourcePath, Action<LSidecarCoreDocument> lSidecarMutate)`

Applies one change to the core record under the cross-process latch.
An unreadable file is left alone, since overwriting it could lose user edits.
A malformed file is moved aside as `.broken` before a fresh record is written.
A record of another media file is replaced by a fresh one.
Scan data an older matched record still embeds is first copied out to its cache file.
JSON fields this build does not know are carried over from the matched record.

## `public static bool LSidecarSectionsSave(`

The list is copied, since the deferred save runs after the caller may have changed it.

## `public static Func<string, LSidecarCoreDocument?> LSidecarCoreSeam { get; set; } = _ => null;`

The librarian's core load, bound by the host, since the store no longer names the Application librarian.
Unattached, it loads nothing, so no sections are restored.

## `public static IReadOnlyList<LSidecarSectionEntry> LSidecarSectionsRead(string lSidecarSourcePath)`

A record that cannot be read yields no sections, and the failure is recorded instead of thrown.
