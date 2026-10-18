using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The roster's gate: the worklist preferences it shows and takes from its boxes. CAtelier makes one per roster; it
// holds the roster's card, detail and menu gates.
// A box left unset reads as unchecked; the engine keeps each preference and answers whether a choice changed it.
public sealed class CRoster
{
    private readonly LRosterPort _cRosterPort;

    internal CRoster(LRosterPort port, CRosterCard card, CRosterDetail detail)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(detail);
        _cRosterPort = port;
        CRosterCard = card;
        CRosterDetail = detail;
        CRosterMenu = new CRosterMenu(port);
    }

    public CRosterCard CRosterCard { get; }

    public CRosterDetail CRosterDetail { get; }

    public CRosterMenu CRosterMenu { get; }

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
        CRosterMenu.CRosterMenuAttach(station);
        CRosterDetail.CRosterDetailAttach(station);
    }
}
