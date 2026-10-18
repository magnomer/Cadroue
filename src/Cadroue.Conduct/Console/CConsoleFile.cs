using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The scene strip's file exchange: the file format the dialogs offer, the file name the window's scene takes, writing
// that scene to a file and loading a scene from one, each failure worded as the warning to show. CConsoleScene makes
// one; the named scene and the window are the scene's, and a loaded scene becomes its state.
public sealed class CConsoleFile
{
    private readonly LConsoleScenePort _cConsoleFilePort;
    private readonly CConsoleScene _cConsoleFileScene;

    internal CConsoleFile(LConsoleScenePort port, CConsoleScene scene)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(scene);
        _cConsoleFilePort = port;
        _cConsoleFileScene = scene;
    }

    // The scene file format the file dialogs offer.
    public string CConsoleSceneExtension => "json";

    // The fallback names the window's scene when the text names none.
    public string CConsoleSceneResolve(string? text, string fallback) =>
        _cConsoleFilePort.LConsoleSceneResolve(
            CConsoleScene.CConsoleSceneParse(text), fallback, _cConsoleFileScene.CConsoleSceneWindow);

    // The warning to show, or null; nothing is written unless the dialog was confirmed.
    public CConsoleText? CConsoleScenePersist(bool? confirmed, string path, string? text, string fallback)
    {
        if (confirmed != true)
        {
            return null;
        }

        return _cConsoleFilePort.LConsoleScenePersist(
                CConsoleScene.CConsoleSceneParse(text), path, fallback, _cConsoleFileScene.CConsoleSceneWindow)
            is { } fault
            ? CConsoleSceneDescribe("Console.Scene.Error.Write", fault)
            : null;
    }

    // The warning to show, or null; the fallback names a scene whose file names none.
    public CConsoleText? CConsoleSceneImport(bool? confirmed, string path, string fallback)
    {
        if (confirmed != true)
        {
            return null;
        }

        (LConsoleSceneAnswer? answer, string? fault) = _cConsoleFilePort.LConsoleSceneImport(
            _cConsoleFileScene.CConsoleSceneName, path, fallback, _cConsoleFileScene.CConsoleSceneWindow);
        if (answer is not null)
        {
            _cConsoleFileScene.CConsoleSceneApply(answer);
            return null;
        }

        return fault is null
            ? CConsoleSceneDescribe("Console.Scene.Error.Invalid", string.Empty)
            : CConsoleSceneDescribe("Console.Scene.Error.Read", fault);
    }

    // A detail-free fault reads its key as plain text.
    private static CConsoleText CConsoleSceneDescribe(string key, string detail) =>
        new(key, detail.Length > 0 ? new object[] { detail } : null);
}
