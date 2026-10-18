using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The roster menu's gate: what an item's menu offers, read at each click, and the restart or cancel it runs.
// CRoster makes one per roster and hands it the roster's station.
public sealed class CRosterMenu
{
    private readonly LRosterMenuPort _cRosterMenuPort;
    private LStation? _cRosterMenuStation;
    private bool _cRosterMenuRestartable;
    private bool _cRosterMenuCancellable;
    private int _cRosterMenuCount;
    private IReadOnlyList<string> _cRosterMenuPaths = [];
    private Action _cRosterMenuAction = () => { };

    internal CRosterMenu(LRosterMenuPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cRosterMenuPort = port;
    }

    public bool CRosterMenuRestartable => _cRosterMenuRestartable;

    public bool CRosterMenuCancellable => _cRosterMenuCancellable;

    public bool CRosterMenuRelay => _cRosterMenuPaths.Count > 0;

    // How many items a restart or cancel acts on, or how many outputs a relay hands on.
    public int CRosterMenuCount => _cRosterMenuCount;

    public IReadOnlyList<string> CRosterMenuPaths => _cRosterMenuPaths;

    // The roster hands its station once; the menu reads the station's selection and cancels on its runner.
    internal void CRosterMenuAttach(LStation station) => _cRosterMenuStation = station;

    // A click on an item reads what its menu offers; a roster not attached to a station offers nothing.
    public void CRosterMenuRead(Guid clicked)
    {
        (_cRosterMenuRestartable, _cRosterMenuCancellable, _cRosterMenuCount, _cRosterMenuPaths, _cRosterMenuAction) =
            _cRosterMenuStation is { } station
                ? _cRosterMenuPort.LRosterMenuRead(station, clicked)
                : (false, false, 0, [], () => { });
    }

    // Runs the restart or cancel the last menu read offered.
    public void CRosterMenuRun() => _cRosterMenuAction();
}
