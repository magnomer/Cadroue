using System;
using System.Threading.Tasks;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The console's decisions over one station of the board: which station it shows, when its progress is set, and each
// command with its question. CAtelier makes one per window. The station handle passes to the engine unread, and the
// engine's resolved station replaces it. The progress is CConsoleProgress's; the status words itself.
public sealed class CConsole
{
    private readonly LConsolePort _cConsolePort;
    private readonly Action _cConsoleSubscription;
    private LStation? _cConsoleStation;
    private bool _cConsolePending;

    internal CConsole(LConsolePort port, LConsoleScenePort scene)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cConsolePort = port;
        _cConsoleSubscription = port.LConsoleAttach(CConsoleChangeHandle, CConsoleDefer, CConsoleItemHandle);
        CConsoleScene = new CConsoleScene(scene);
    }

    // The window's scene strip, beside the board.
    public CConsoleScene CConsoleScene { get; }

    // Told when the status may differ now: the schedule, the board, the station shown or its automatic run changed.
    public event Action? CConsoleChanged;

    // Told once per batch of record changes until the next update; the window updates on a later turn.
    public event Action? CConsoleDeferred;

    // The shown station's progress.
    public CConsoleProgress CConsoleProgress { get; } = new();

    public TimeSpan CConsolePeriod => TimeSpan.FromSeconds(_cConsolePort.LConsolePeriodRead());

    public void CConsoleClose() => _cConsoleSubscription();

    public void CConsoleLoad() => _cConsolePort.LConsoleLoad();

    // The progress follows the mean of the station's running records.
    public CConsoleStatus CConsoleUpdate()
    {
        _cConsolePending = false;
        LConsoleAnswer answer = _cConsolePort.LConsoleRead(_cConsoleStation);
        _cConsoleStation = answer.LConsoleStation;
        CConsoleProgress.CConsoleProgressSet(answer.LConsoleProgress);
        return CConsoleStatus.CConsoleStatusCreate(answer);
    }

    // A station equal to the one held is not told.
    public void CConsoleStationSet(LStation station)
    {
        if (ReferenceEquals(_cConsoleStation, station))
        {
            return;
        }

        _cConsoleStation = station;
        CConsoleChanged?.Invoke();
    }

    public void CConsoleStationMove(int step)
    {
        if (_cConsolePort.LConsoleStationMove(_cConsoleStation, step) is not { } next)
        {
            return;
        }

        _cConsoleStation = next;
        CConsoleChanged?.Invoke();
    }

    public void CConsoleStart() => _cConsoleStation = _cConsolePort.LConsoleStart(_cConsoleStation);

    public void CConsolePause() => _cConsoleStation = _cConsolePort.LConsolePause(_cConsoleStation);

    public void CConsoleStop() => _cConsoleStation = _cConsolePort.LConsoleStop(_cConsoleStation);

    // A box left unset reads as unchecked; only a change is told.
    public void CConsoleAutoSet(bool? chosen)
    {
        (LStation station, bool changed) = _cConsolePort.LConsoleAutoSave(_cConsoleStation, chosen == true);
        _cConsoleStation = station;
        if (changed)
        {
            CConsoleChanged?.Invoke();
        }
    }

    // Asked whatever the destructive-confirm preference says, naming the one running output or the running count;
    // nothing is asked while none runs.
    public void CConsoleCancel(CEnvoy envoy)
    {
        ArgumentNullException.ThrowIfNull(envoy);
        _cConsoleStation = _cConsolePort.LConsoleCancel(
            _cConsoleStation,
            (count, name) => envoy.CEnvoyConfirm(
                "Console.Cancel.Confirm",
                count > 1 ? "Console.Cancel.MultipleSubject" : "Console.Cancel.SingleSubject",
                count > 1 ? count : (object)name,
                "Terms.Stop",
                "Console.Cancel.Title",
                "Console.Cancel.Keep"));
    }

    // The warning to show, or null when every removable record went.
    public CConsoleText? CConsoleRemove()
    {
        (LStation station, int total, int held, int blocked) = _cConsolePort.LConsoleRemove(_cConsoleStation);
        _cConsoleStation = station;
        return CConsoleRemovalCreate(total, held, blocked);
    }

    public void CConsoleDoneClear(CEnvoy envoy)
    {
        ArgumentNullException.ThrowIfNull(envoy);
        if (envoy.CEnvoyDestructiveConfirm("Console.ClearDone.Confirm", "Terms.Remove", "Console.Confirm.Title"))
        {
            _cConsolePort.LConsoleDoneClear();
        }
    }

    // While any station runs, the stop question is asked whatever the preference says.
    public Task CConsoleAllClear(CEnvoy envoy)
    {
        ArgumentNullException.ThrowIfNull(envoy);
        bool confirmed = _cConsolePort.LConsoleActiveCheck()
            ? envoy.CEnvoyConfirm("Console.ClearAll.RunningConfirm", "Terms.Stop", "Console.Confirm.Title")
            : envoy.CEnvoyDestructiveConfirm("Console.ClearAll.Confirm", "Terms.Remove", "Console.Confirm.Title");
        return confirmed ? _cConsolePort.LConsoleAllClear() : Task.CompletedTask;
    }

    // Whether the window clears its tabs.
    public bool CConsoleTabsClear(CEnvoy envoy)
    {
        ArgumentNullException.ThrowIfNull(envoy);
        return envoy.CEnvoyDestructiveConfirm("Console.ClearTabs.Confirm", "Terms.Remove", "Console.Confirm.Title");
    }

    private void CConsoleChangeHandle() => CConsoleChanged?.Invoke();

    private void CConsoleDefer()
    {
        if (_cConsolePending)
        {
            return;
        }

        _cConsolePending = true;
        CConsoleDeferred?.Invoke();
    }

    private void CConsoleItemHandle(Predicate<LStation> owner, double fraction)
    {
        _cConsoleStation = _cConsolePort.LConsoleStationResolve(_cConsoleStation);
        if (owner(_cConsoleStation))
        {
            CConsoleProgress.CConsoleProgressSet(fraction);
        }
    }

    private static CConsoleText? CConsoleRemovalCreate(int total, int held, int blocked)
    {
        if (held == 0 && blocked == 0)
        {
            return null;
        }

        CConsoleText detail = held > 0 && blocked > 0
            ? CConsoleText.CConsoleTextCreate("Console.Remove.HeldAndBlocked", held, blocked)
            : held > 0
                ? CConsoleText.CConsoleTextCreate("Console.Remove.Held", held)
                : CConsoleText.CConsoleTextCreate("Console.Remove.Blocked", blocked);
        return CConsoleText.CConsoleTextCreate("Console.Remove.Partial", total - held - blocked, total, detail);
    }
}
