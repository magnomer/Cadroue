using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The window's scene under a name, as the window shows it now.
public delegate LSceneDocument LConsoleSnapshot(string lName);

// Rebuilds the window from a scene once its tabs may close, closing them first through the closure; false when
// they may not.
public delegate bool LConsoleRestorer(LSceneDocument lScene, bool lConfirmed, Action lClosure);

// The window side of the scene strip, built by the gate from the window's callables and handed on unread.
public sealed record LConsoleWindow(
    LConsoleSnapshot LConsoleWindowSnapshot, LConsoleRestorer LConsoleWindowRestorer, Action LConsoleWindowClosure);

// Every member takes the gate's scene name, matched to the catalogue ignoring case, and the window, null until the
// window attaches; each answers the scene state after it.
public interface LConsoleScenePort
{
    // The active scene becomes the named one.
    LConsoleSceneAnswer LConsoleSceneStart(LConsoleWindow? lWindow);

    // Reloads the catalogue and answers its names.
    LConsoleSceneAnswer LConsoleSceneRebuild(string lName, LConsoleWindow? lWindow);

    LConsoleSceneAnswer LConsoleSceneRead(string lName, LConsoleWindow? lWindow);

    // Asks only for a stored target, then whether the tabs may close, and traces a declined close; an applied target
    // becomes the active, named scene.
    LConsoleSceneAnswer LConsoleSceneLoad(
        string lName, string lTarget, LConsoleWindow? lWindow, Func<bool> lLoadConfirm, Func<bool> lCloseConfirm);

    // Null when the catalogue held no such scene; deleting the named scene clears the name and the active scene.
    LConsoleSceneAnswer? LConsoleSceneDelete(string lName, string lTarget, LConsoleWindow? lWindow);

    // Stores the window under the name and makes it the active, named scene; null without a window.
    LConsoleSceneAnswer? LConsoleSceneSave(string lName, LConsoleWindow? lWindow);

    // The file name for the stored scene, else for the window under the name or the fallback.
    string LConsoleSceneResolve(string lName, string lFallback, LConsoleWindow? lWindow);

    // Writes the scene the file name is resolved from; answers the write failure's message, or null.
    string? LConsoleScenePersist(string lName, string lPath, string lFallback, LConsoleWindow? lWindow);

    // Stores the file's scene under its own name, the file stem or the fallback, numbered past the catalogue's names.
    // Answers the read failure's message, or the state; both null when the file holds no scene.
    (LConsoleSceneAnswer? LConsoleSceneImported, string? LConsoleSceneFault) LConsoleSceneImport(
        string lName, string lPath, string lFallback, LConsoleWindow? lWindow);
}

// The named scene, whether the catalogue stores it, whether the window left it, and the catalogue's names when they
// were reloaded.
public sealed record LConsoleSceneAnswer(
    string LConsoleSceneName,
    bool LConsoleSceneStored,
    bool LConsoleSceneDirty,
    IReadOnlyList<string>? LConsoleSceneNames);
