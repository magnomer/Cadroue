using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The roster's gate: the worklist preferences it shows and takes from its boxes, and what an item's menu offers.
// CAtelier makes one per roster.
// A box left unset reads as unchecked; the engine keeps each preference and answers whether a choice changed it.
public sealed class CRoster
{
    private readonly LRosterPort _cRosterPort;
    private LStation? _cRosterStation;
    private bool _cRosterMenuRestartable;
    private bool _cRosterMenuCancellable;
    private int _cRosterMenuCount;
    private IReadOnlyList<string> _cRosterMenuPaths = [];
    private Action _cRosterMenuAction = () => { };

    internal CRoster(LRosterPort port, CRosterCard card, CRosterDetail detail)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(detail);
        _cRosterPort = port;
        CRosterCard = card;
        CRosterDetail = detail;
    }

    public CRosterCard CRosterCard { get; }

    public CRosterDetail CRosterDetail { get; }

    // Told when completed batches start to collapse, so the cards are built again.
    public event Action? CRosterChanged;

    public bool CRosterShared => _cRosterPort.LRosterSharedRead();

    public bool CRosterDone => _cRosterPort.LRosterDoneRead();

    public bool CRosterSharedSet(bool? chosen) => _cRosterPort.LRosterSharedSave(chosen == true);

    public bool CRosterDoneSet(bool? chosen)
    {
        bool done = chosen == true;
        if (!_cRosterPort.LRosterDoneSave(done))
        {
            return false;
        }

        if (done)
        {
            CRosterChanged?.Invoke();
        }

        return true;
    }

    // The roster hands its station once; the menu reads the station's selection and cancels on its runner, and the
    // detail tells ownership against that runner.
    public void CRosterAttach(LStation station)
    {
        ArgumentNullException.ThrowIfNull(station);
        _cRosterStation = station;
        CRosterDetail.CRosterDetailAttach(station);
    }

    public bool CRosterMenuRestartable => _cRosterMenuRestartable;

    public bool CRosterMenuCancellable => _cRosterMenuCancellable;

    public bool CRosterMenuRelay => _cRosterMenuPaths.Count > 0;

    // How many items a restart or cancel acts on, or how many outputs a relay hands on.
    public int CRosterMenuCount => _cRosterMenuCount;

    public IReadOnlyList<string> CRosterMenuPaths => _cRosterMenuPaths;

    // A click on an item reads what its menu offers; a roster not attached to a station offers nothing.
    public void CRosterMenuRead(Guid clicked)
    {
        (_cRosterMenuRestartable, _cRosterMenuCancellable, _cRosterMenuCount, _cRosterMenuPaths, _cRosterMenuAction) =
            _cRosterStation is { } station
                ? _cRosterPort.LRosterMenuRead(station, clicked)
                : (false, false, 0, [], () => { });
    }

    // Runs the restart or cancel the last menu read offered.
    public void CRosterMenuRun() => _cRosterMenuAction();
}
