namespace Cadroue.ShellEngine;

// Every member takes the console's station handle, null or one that left the board, and answers the station it
// resolved, so the gate keeps the handle the engine chose.
public interface LConsolePort
{
    // Tells changed when the schedule or the station board changes, deferred when a record changes beyond its
    // progress, and progressed with the record's owner test and fraction when only its progress moves. The answer
    // detaches all three.
    Action LConsoleAttach(Action lChanged, Action lDeferred, Action<Predicate<LStation>, double> lProgressed);

    void LConsoleLoad();

    LConsoleAnswer LConsoleRead(LStation? lStation);

    LStation LConsoleStationResolve(LStation? lStation);

    // The station one step along the board, wrapping; null while the board holds one station.
    LStation? LConsoleStationMove(LStation? lStation, int lStep);

    LStation LConsoleStart(LStation? lStation);

    LStation LConsolePause(LStation? lStation);

    LStation LConsoleStop(LStation? lStation);

    (LStation LConsoleStation, bool LConsoleChanged) LConsoleAutoSave(LStation? lStation, bool lAuto);

    // While any of the station's records runs, asks with their count and the first one's output name; a yes cancels
    // them all and turns the station's automatic run off.
    LStation LConsoleCancel(LStation? lStation, Func<int, string, bool> lConfirm);

    // Removes the station's removable selection; a selection with nothing removable answers zero counts.
    (LStation LConsoleStation, int LConsoleTotal, int LConsoleHeld, int LConsoleBlocked) LConsoleRemove(
        LStation? lStation);

    void LConsoleDoneClear();

    bool LConsoleActiveCheck();

    Task LConsoleAllClear();

    double LConsolePeriodRead();
}

// One station's schedule facts: its runner state, the schedule counts, its running records (count, mean progress,
// and the first one's output name, stage name and fraction), its selection and the records, and its board place.
public sealed record LConsoleAnswer(
    LStation LConsoleStation,
    bool LConsoleRunning,
    bool LConsoleSuspended,
    bool LConsoleAuto,
    int LConsoleTotal,
    int LConsoleDone,
    int LConsolePending,
    int LConsoleWorking,
    double LConsoleProgress,
    string LConsoleName,
    string LConsoleStage,
    double LConsoleFraction,
    bool LConsoleRemovable,
    bool LConsoleFinished,
    bool LConsoleClearable,
    string LConsoleLabel,
    bool LConsoleBackground,
    int LConsoleNumber,
    int LConsoleCount);
