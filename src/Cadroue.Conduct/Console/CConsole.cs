using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One text the window words: a wording key and its arguments, each a number, a ready string or another text.
// Without arguments (null) the key is read as plain text; with them it is formatted.
public sealed record CConsoleText(string CConsoleTextKey, IReadOnlyList<object>? CConsoleTextArguments);

// The console's ready status: the line and its accented part, the command switches, and the station text, which is
// a CConsoleText or the station's own label.
public sealed record CConsoleStatus(
    CConsoleText CConsoleStatusLine,
    CConsoleText? CConsoleStatusAccent,
    bool CConsoleStatusRunning,
    bool CConsoleStatusLoaded,
    bool CConsoleStatusBusy,
    bool CConsoleStatusActive,
    bool CConsoleStatusRemovable,
    bool CConsoleStatusDone,
    bool CConsoleStatusClearable,
    bool CConsoleStatusBoard,
    bool CConsoleStatusAuto,
    object CConsoleStatusStation);

// The console's decisions over one station of the board: which station it shows, the progress it glides to, the
// status it words, and each command with its question. CAtelier makes one per window. The station handle passes to
// the engine unread, and the engine's resolved station replaces it.
public sealed class CConsole
{
    private readonly LConsolePort _cConsolePort;
    private readonly Action _cConsoleSubscription;
    private LStation? _cConsoleStation;
    private bool _cConsolePending;
    private bool _cConsoleBackward;
    private double _cConsoleProgress;

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

    public event Action? CConsoleProgressChanged;

    // The progress fraction, held inside 0 to 1.
    public double CConsoleProgress => _cConsoleProgress;

    // The bar glides to a forward fraction above zero and jumps otherwise.
    public bool CConsoleGlide => _cConsoleProgress > 0 && !_cConsoleBackward;

    public TimeSpan CConsolePeriod => TimeSpan.FromSeconds(_cConsolePort.LConsolePeriodRead());

    public void CConsoleClose() => _cConsoleSubscription();

    public void CConsoleLoad() => _cConsolePort.LConsoleLoad();

    // The progress follows the mean of the station's running records.
    public CConsoleStatus CConsoleUpdate()
    {
        _cConsolePending = false;
        LConsoleAnswer answer = _cConsolePort.LConsoleRead(_cConsoleStation);
        _cConsoleStation = answer.LConsoleStation;
        CConsoleProgressSet(answer.LConsoleProgress);
        return CConsoleStatusCreate(answer);
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
            CConsoleProgressSet(fraction);
        }
    }

    private void CConsoleProgressSet(double target)
    {
        double clamped = Math.Clamp(target, 0, 1);
        if (clamped.Equals(_cConsoleProgress))
        {
            return;
        }

        _cConsoleBackward = clamped < _cConsoleProgress;
        _cConsoleProgress = clamped;
        CConsoleProgressChanged?.Invoke();
    }

    private static CConsoleText CConsoleTextCreate(string key, params object[] arguments) =>
        new(key, arguments.Length == 0 ? null : arguments);

    private static CConsoleText? CConsoleRemovalCreate(int total, int held, int blocked)
    {
        if (held == 0 && blocked == 0)
        {
            return null;
        }

        CConsoleText detail = held > 0 && blocked > 0
            ? CConsoleTextCreate("Console.Remove.HeldAndBlocked", held, blocked)
            : held > 0
                ? CConsoleTextCreate("Console.Remove.Held", held)
                : CConsoleTextCreate("Console.Remove.Blocked", blocked);
        return CConsoleTextCreate("Console.Remove.Partial", total - held - blocked, total, detail);
    }

    // Paused means neither running nor suspended; a loaded paused console accents its state.
    private static CConsoleStatus CConsoleStatusCreate(LConsoleAnswer answer)
    {
        bool paused = !answer.LConsoleRunning && !answer.LConsoleSuspended;
        CConsoleText state = CConsoleTextCreate(
            answer.LConsoleSuspended
                ? "Console.State.Suspended"
                : answer.LConsoleRunning ? "Console.State.Running" : "Console.State.Paused");
        return new CConsoleStatus(
            CConsoleLineCreate(answer, state),
            answer.LConsoleTotal > 0 && paused ? state : null,
            answer.LConsoleRunning,
            answer.LConsoleTotal > 0,
            answer.LConsoleWorking > 0,
            answer.LConsoleRunning || answer.LConsoleSuspended,
            answer.LConsoleRemovable,
            answer.LConsoleFinished,
            answer.LConsoleClearable,
            answer.LConsoleCount > 1,
            answer.LConsoleAuto,
            CConsoleStationCreate(answer));
    }

    private static CConsoleText CConsoleLineCreate(LConsoleAnswer answer, CConsoleText state)
    {
        if (answer.LConsoleTotal == 0)
        {
            return CConsoleTextCreate("Console.Status.Empty");
        }

        CConsoleText done = CConsoleTextCreate("Console.Done.Format", answer.LConsoleDone, answer.LConsoleTotal);
        if (answer.LConsoleWorking == 0)
        {
            return CConsoleTextCreate("Console.Status.Pending", state, done, answer.LConsolePending);
        }

        if (answer.LConsoleWorking > 1)
        {
            return CConsoleTextCreate(
                "Console.Status.MultipleRunning", answer.LConsoleWorking, answer.LConsoleProgress, done);
        }

        return CConsoleTextCreate(
            "Console.Status.Running",
            answer.LConsoleName,
            done,
            CConsoleStageCreate(answer.LConsoleStage),
            answer.LConsoleFraction);
    }

    // The stage arrives by the engine's stage name; an unnamed stage reads as encoding.
    private static CConsoleText CConsoleStageCreate(string stage) =>
        CConsoleTextCreate(stage switch
        {
            "LWorkStageExtract" => "Console.Stage.Extract",
            "LWorkStageAnalyze" => "Console.Stage.Analyze",
            "LWorkStageProcess" => "Console.Stage.Process",
            "LWorkStageMux" => "Console.Stage.Mux",
            "LWorkStagePassthrough" => "Console.Stage.Copy",
            _ => "Console.Stage.Encode"
        });

    // The background station reads by its wording; a board of several numbers the station.
    private static object CConsoleStationCreate(LConsoleAnswer answer)
    {
        object label = answer.LConsoleBackground
            ? CConsoleTextCreate("Console.Station.Background")
            : answer.LConsoleLabel;
        return answer.LConsoleCount > 1
            ? CConsoleTextCreate("Console.Station.Numbered", label, answer.LConsoleNumber, answer.LConsoleCount)
            : label;
    }
}
