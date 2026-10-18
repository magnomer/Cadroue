using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The console's ready status: the line and its accented part, the command switches, and the station text, which is
// a CConsoleText or the station's own label. It is worded here from the engine's answer.
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
    object CConsoleStatusStation)
{
    // Paused means neither running nor suspended; a loaded paused console accents its state.
    internal static CConsoleStatus CConsoleStatusCreate(LConsoleAnswer answer)
    {
        bool paused = !answer.LConsoleRunning && !answer.LConsoleSuspended;
        CConsoleText state = CConsoleText.CConsoleTextCreate(
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
            return CConsoleText.CConsoleTextCreate("Console.Status.Empty");
        }

        CConsoleText done = CConsoleText.CConsoleTextCreate(
            "Console.Done.Format", answer.LConsoleDone, answer.LConsoleTotal);
        if (answer.LConsoleWorking == 0)
        {
            return CConsoleText.CConsoleTextCreate("Console.Status.Pending", state, done, answer.LConsolePending);
        }

        if (answer.LConsoleWorking > 1)
        {
            return CConsoleText.CConsoleTextCreate(
                "Console.Status.MultipleRunning", answer.LConsoleWorking, answer.LConsoleProgress, done);
        }

        return CConsoleText.CConsoleTextCreate(
            "Console.Status.Running",
            answer.LConsoleName,
            done,
            CConsoleStageCreate(answer.LConsoleStage),
            answer.LConsoleFraction);
    }

    // The stage arrives by the engine's stage name; an unnamed stage reads as encoding.
    private static CConsoleText CConsoleStageCreate(string stage) =>
        CConsoleText.CConsoleTextCreate(stage switch
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
            ? CConsoleText.CConsoleTextCreate("Console.Station.Background")
            : answer.LConsoleLabel;
        return answer.LConsoleCount > 1
            ? CConsoleText.CConsoleTextCreate(
                "Console.Station.Numbered", label, answer.LConsoleNumber, answer.LConsoleCount)
            : label;
    }
}
