using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The scene strip's decisions over one window: the scene it names, the one a closing list reloads, whether the
// catalogue stores the named scene and whether the window left it, the catalogue's names, and each command with its
// question. The window hands its scene read and apply in at attach; they pass to the engine unread. The file exchange
// is CConsoleFile's.
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
        CConsoleSceneFile = new CConsoleFile(port, this);
    }

    // Told when the catalogue's names were reloaded.
    public event Action? CConsoleSceneListed;

    // Told when the named scene, its stored state or its mark may differ.
    public event Action? CConsoleSceneChanged;

    // Told when only the mark may differ.
    public event Action? CConsoleSceneMarked;

    // The scene strip's file exchange.
    public CConsoleFile CConsoleSceneFile { get; }

    public IReadOnlyList<string> CConsoleSceneNames => _cConsoleSceneNames;

    public string CConsoleSceneName => _cConsoleSceneName;

    // The named scene while the catalogue stores it, else null.
    public string? CConsoleSceneSelected => _cConsoleSceneStored ? _cConsoleSceneName : null;

    // Whether the window no longer matches the named, stored scene.
    public bool CConsoleSceneDirty => _cConsoleSceneDirty;

    // The scene a closing list loads, once.
    public string? CConsoleSceneReload => _cConsoleSceneReload;

    // The window the window side attached, passed to the engine unread.
    internal LConsoleWindow? CConsoleSceneWindow => _cConsoleSceneWindow;

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

    internal void CConsoleSceneApply(LConsoleSceneAnswer answer)
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

    internal static string CConsoleSceneParse(string? text) => (text ?? string.Empty).Trim();
}
