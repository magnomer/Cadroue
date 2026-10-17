using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The scene strip's decisions over one window: the scene it names, the one a closing list reloads, whether the
// catalogue stores the named scene and whether the window left it, the catalogue's names, and each command with its
// question. The window hands its scene read and apply in at attach; they pass to the engine unread.
public sealed class CConsoleScene
{
    private readonly LConsoleScenePort _cConsoleScenePort;
    private LConsoleWindow? _cConsoleSceneWindow;
    private Func<bool> _cConsoleSceneBusy = static () => false;
    private string _cConsoleSceneName = string.Empty;
    private string? _cConsoleSceneReload;
    private bool _cConsoleSceneStored;
    private bool _cConsoleSceneDirty;
    private IReadOnlyList<string> _cConsoleSceneNames = Array.Empty<string>();

    internal CConsoleScene(LConsoleScenePort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cConsoleScenePort = port;
    }

    // Told when the catalogue's names were reloaded.
    public event Action? CConsoleSceneListed;

    // Told when the named scene, its stored state or its mark may differ.
    public event Action? CConsoleSceneChanged;

    // Told when only the mark may differ.
    public event Action? CConsoleSceneMarked;

    public IReadOnlyList<string> CConsoleSceneNames => _cConsoleSceneNames;

    public string CConsoleSceneName => _cConsoleSceneName;

    // The named scene while the catalogue stores it, else null.
    public string? CConsoleSceneSelected => _cConsoleSceneStored ? _cConsoleSceneName : null;

    // Whether the window no longer matches the named, stored scene.
    public bool CConsoleSceneDirty => _cConsoleSceneDirty;

    // The scene a closing list loads, once.
    public string? CConsoleSceneReload => _cConsoleSceneReload;

    // The scene file format the file dialogs offer.
    public string CConsoleSceneExtension => "json";

    // The window's scene read and apply, its closing of every tab, and whether any tab is busy.
    public void CConsoleSceneAttach(
        LConsoleSnapshot snapshot, LConsoleRestorer restorer, Action closure, Func<bool> busy)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(restorer);
        ArgumentNullException.ThrowIfNull(closure);
        ArgumentNullException.ThrowIfNull(busy);
        _cConsoleSceneWindow = new LConsoleWindow(snapshot, restorer, closure);
        _cConsoleSceneBusy = busy;
    }

    public void CConsoleSceneStart() =>
        CConsoleSceneApply(_cConsoleScenePort.LConsoleSceneStart(_cConsoleSceneWindow));

    public void CConsoleSceneRebuild() =>
        CConsoleSceneApply(_cConsoleScenePort.LConsoleSceneRebuild(_cConsoleSceneName, _cConsoleSceneWindow));

    public void CConsoleSceneUpdate() =>
        CConsoleSceneApply(_cConsoleScenePort.LConsoleSceneRead(_cConsoleSceneName, _cConsoleSceneWindow));

    // The mark alone follows the window.
    public void CConsoleSceneCheck()
    {
        _cConsoleSceneDirty =
            _cConsoleScenePort.LConsoleSceneRead(_cConsoleSceneName, _cConsoleSceneWindow).LConsoleSceneDirty;
        CConsoleSceneMarked?.Invoke();
    }

    // The mark follows the window while a scene is named and the list holds no focus.
    public void CConsoleSceneTick(bool focusWithin)
    {
        if (_cConsoleSceneName.Length == 0 || focusWithin)
        {
            return;
        }

        CConsoleSceneCheck();
    }

    public void CConsoleSceneSet(string? name) => _cConsoleSceneReload = name;

    // A row that carries no scene keeps the one held.
    public void CConsoleSceneAccept(string? name)
    {
        if (name is not null)
        {
            _cConsoleSceneReload = name;
        }
    }

    // A pick other than the named scene loads at once, or on the list's close while the list is open.
    public void CConsoleSceneSelect(object? selected, bool dropOpen, CEnvoy envoy)
    {
        ArgumentNullException.ThrowIfNull(envoy);
        if (selected is not string name || string.Equals(_cConsoleSceneName, name, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (dropOpen)
        {
            _cConsoleSceneReload = name;
            return;
        }

        _cConsoleSceneReload = null;
        CConsoleSceneLoad(name, envoy);
    }

    // The list's close loads the held scene once, else repaints.
    public void CConsoleSceneClose(CEnvoy envoy)
    {
        ArgumentNullException.ThrowIfNull(envoy);
        string? reload = _cConsoleSceneReload;
        _cConsoleSceneReload = null;
        if (reload is not null)
        {
            CConsoleSceneLoad(reload, envoy);
            return;
        }

        CConsoleSceneUpdate();
    }

    // False when no row was the source; the reload is dropped either way.
    public bool CConsoleSceneDelete(string? name)
    {
        if (name is null)
        {
            return false;
        }

        _cConsoleSceneReload = null;
        if (_cConsoleScenePort.LConsoleSceneDelete(_cConsoleSceneName, name, _cConsoleSceneWindow) is { } answer)
        {
            CConsoleSceneApply(answer);
        }

        return true;
    }

    // False when the text names no scene once trimmed: the window tells that a name is required.
    public bool CConsoleSceneSave(string? text)
    {
        string name = CConsoleSceneParse(text);
        if (name.Length == 0)
        {
            return false;
        }

        if (_cConsoleScenePort.LConsoleSceneSave(name, _cConsoleSceneWindow) is { } answer)
        {
            CConsoleSceneApply(answer);
        }

        return true;
    }

    // The fallback names the window's scene when the text names none.
    public string CConsoleSceneResolve(string? text, string fallback) =>
        _cConsoleScenePort.LConsoleSceneResolve(CConsoleSceneParse(text), fallback, _cConsoleSceneWindow);

    // The warning to show, or null; nothing is written unless the dialog was confirmed.
    public CConsoleText? CConsoleScenePersist(bool? confirmed, string path, string? text, string fallback)
    {
        if (confirmed != true)
        {
            return null;
        }

        return _cConsoleScenePort.LConsoleScenePersist(CConsoleSceneParse(text), path, fallback, _cConsoleSceneWindow)
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

        (LConsoleSceneAnswer? answer, string? fault) =
            _cConsoleScenePort.LConsoleSceneImport(_cConsoleSceneName, path, fallback, _cConsoleSceneWindow);
        if (answer is not null)
        {
            CConsoleSceneApply(answer);
            return null;
        }

        return fault is null
            ? CConsoleSceneDescribe("Console.Scene.Error.Invalid", string.Empty)
            : CConsoleSceneDescribe("Console.Scene.Error.Read", fault);
    }

    // Asked whatever the destructive-confirm preference says; the busy question follows only while a tab works.
    private void CConsoleSceneLoad(string target, CEnvoy envoy) =>
        CConsoleSceneApply(
            _cConsoleScenePort.LConsoleSceneLoad(
                _cConsoleSceneName,
                target,
                _cConsoleSceneWindow,
                () => envoy.CEnvoyConfirm(
                    "Console.Scene.LoadConfirm", "Terms.Load", "Console.Scene.LoadTitle", target),
                () => !_cConsoleSceneBusy()
                    || envoy.CEnvoyConfirm("Tab.Close.BusyAllMessage", "Terms.Stop", "Tab.Close.BusyTitle")));

    private void CConsoleSceneApply(LConsoleSceneAnswer answer)
    {
        _cConsoleSceneName = answer.LConsoleSceneName;
        _cConsoleSceneStored = answer.LConsoleSceneStored;
        _cConsoleSceneDirty = answer.LConsoleSceneDirty;
        if (answer.LConsoleSceneNames is { } names)
        {
            _cConsoleSceneNames = names;
            CConsoleSceneListed?.Invoke();
        }

        CConsoleSceneChanged?.Invoke();
    }

    private static string CConsoleSceneParse(string? text) => (text ?? string.Empty).Trim();

    // A detail-free fault reads its key as plain text.
    private static CConsoleText CConsoleSceneDescribe(string key, string detail) =>
        new(key, detail.Length > 0 ? new object[] { detail } : null);
}
