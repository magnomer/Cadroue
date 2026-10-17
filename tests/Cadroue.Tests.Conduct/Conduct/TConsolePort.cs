using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Cadroue.Conduct;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// The console's two ports in one fake: the engine side answers what a test sets, and the ask side answers
// TConsoleConfirmed. Every call lands in TConsoleCalls in order, and every station handle in TConsoleHandles.
internal sealed class TConsolePort : LConsolePort, CEnvoy
{
    public TConsolePort() => TConsoleAnswer = TConsoleAnswerCreate(TConsoleResolved);

    public LStation TConsoleResolved { get; set; } = TConsoleStationCreate();

    public LStation? TConsoleMoved { get; set; }

    public LConsoleAnswer TConsoleAnswer { get; set; }

    public IReadOnlyList<Guid> TConsoleIds { get; set; } = [];

    public string TConsoleName { get; set; } = string.Empty;

    public (int TConsoleTotal, int TConsoleHeld, int TConsoleBlocked) TConsoleRemoval { get; set; }

    public bool TConsoleAutoChanged { get; set; }

    public bool TConsoleActive { get; set; }

    public bool TConsoleConfirmed { get; set; }

    public List<string> TConsoleCalls { get; } = [];

    public List<LStation?> TConsoleHandles { get; } = [];

    public List<object?[]> TConsoleAsks { get; } = [];

    public Action? TConsoleChanged { get; private set; }

    public Action? TConsoleDeferred { get; private set; }

    public Action<Predicate<LStation>, double>? TConsoleProgressed { get; private set; }

    // A station handle the gate passes unread; the engine's constructor needs a running schedule, so none runs here.
    public static LStation TConsoleStationCreate() =>
        (LStation)RuntimeHelpers.GetUninitializedObject(typeof(LStation));

    public static LConsoleAnswer TConsoleAnswerCreate(LStation station) =>
        new(station, false, false, false, 0, 0, 0, 0, 0, string.Empty, string.Empty, 0, false, false, false,
            string.Empty, false, 1, 1);

    public Action LConsoleAttach(Action lChanged, Action lDeferred, Action<Predicate<LStation>, double> lProgressed)
    {
        TConsoleCalls.Add("attach");
        TConsoleChanged = lChanged;
        TConsoleDeferred = lDeferred;
        TConsoleProgressed = lProgressed;
        return () => TConsoleCalls.Add("detach");
    }

    public void LConsoleLoad() => TConsoleCalls.Add("load");

    public LConsoleAnswer LConsoleRead(LStation? lStation)
    {
        TConsoleCallRecord("read", lStation);
        return TConsoleAnswer;
    }

    public LStation LConsoleStationResolve(LStation? lStation) => TConsoleCallRecord("resolve", lStation);

    public LStation? LConsoleStationMove(LStation? lStation, int lStep)
    {
        TConsoleCallRecord("move " + lStep, lStation);
        return TConsoleMoved;
    }

    public LStation LConsoleStart(LStation? lStation) => TConsoleCallRecord("start", lStation);

    public LStation LConsolePause(LStation? lStation) => TConsoleCallRecord("pause", lStation);

    public LStation LConsoleStop(LStation? lStation) => TConsoleCallRecord("stop", lStation);

    public (LStation LConsoleStation, bool LConsoleChanged) LConsoleAutoSave(LStation? lStation, bool lAuto) =>
        (TConsoleCallRecord("auto " + lAuto, lStation), TConsoleAutoChanged);

    public LStation LConsoleCancel(LStation? lStation, Func<int, string, bool> lConfirm)
    {
        TConsoleCallRecord("cancel", lStation);
        if (TConsoleIds.Count > 0 && lConfirm(TConsoleIds.Count, TConsoleName))
        {
            TConsoleCalls.Add("cancel " + TConsoleIds.Count);
        }

        return TConsoleResolved;
    }

    public (LStation LConsoleStation, int LConsoleTotal, int LConsoleHeld, int LConsoleBlocked) LConsoleRemove(
        LStation? lStation) =>
        (TConsoleCallRecord("remove", lStation), TConsoleRemoval.TConsoleTotal, TConsoleRemoval.TConsoleHeld,
            TConsoleRemoval.TConsoleBlocked);

    public void LConsoleDoneClear() => TConsoleCalls.Add("done");

    public bool LConsoleActiveCheck()
    {
        TConsoleCalls.Add("active");
        return TConsoleActive;
    }

    public Task LConsoleAllClear()
    {
        TConsoleCalls.Add("all");
        return Task.CompletedTask;
    }

    public double LConsolePeriodRead() => 0.5;

    public bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey) =>
        TConsoleAskRecord("confirm", questionKey, actionKey, titleKey);

    public bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey, object? argument) =>
        TConsoleAskRecord("confirm", questionKey, actionKey, titleKey, argument);

    public bool CEnvoyConfirm(
        string questionKey, string subjectKey, object? subject, string actionKey, string titleKey, string dismissKey) =>
        TConsoleAskRecord("confirm", questionKey, subjectKey, subject, actionKey, titleKey, dismissKey);

    public bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey) =>
        TConsoleAskRecord("destructive", questionKey, actionKey, titleKey);

    public bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey, object? argument) =>
        TConsoleAskRecord("destructive", questionKey, actionKey, titleKey, argument);

    public void CEnvoyWarningShow(string titleKey, string messageKey) =>
        TConsoleAskRecord("warning", titleKey, messageKey);

    public void CEnvoyFailureShow(string titleKey, string messageKey) =>
        TConsoleAskRecord("failure", titleKey, messageKey);

    private LStation TConsoleCallRecord(string call, LStation? station)
    {
        TConsoleCalls.Add(call);
        TConsoleHandles.Add(station);
        return TConsoleResolved;
    }

    private bool TConsoleAskRecord(params object?[] ask)
    {
        TConsoleCalls.Add((string)ask[0]!);
        TConsoleAsks.Add(ask);
        return TConsoleConfirmed;
    }
}
