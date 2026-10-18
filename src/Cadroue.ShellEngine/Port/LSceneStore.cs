using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The scene catalogue calls the host hands in, each one member of the scene store: reload, names, the active name
// and its setter, the stored scene by name, the scene comparison, save, delete, the file name, file write and read,
// the file stem, and the loading trace.
public sealed record LSceneStore(
    Action LSceneStoreCatalogue,
    Func<IReadOnlyList<string>> LSceneStoreNames,
    Func<string> LSceneStoreActive,
    Action<string> LSceneStoreActivation,
    Func<string, LSceneDocument?> LSceneStoreEntry,
    Func<LSceneDocument, LSceneDocument, bool> LSceneStoreEquality,
    Func<LSceneDocument, bool> LSceneStoreKeeper,
    Func<string, bool> LSceneStoreRemoval,
    Func<string, string, string> LSceneStoreFile,
    Action<LSceneDocument, string> LSceneStoreExport,
    Func<string, LSceneDocument?> LSceneStoreSource,
    Func<string, string, string> LSceneStoreStem,
    Action<string> LSceneStoreLoading);
