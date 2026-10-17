using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// The scene strip's two ports in one fake: the engine side answers TConsoleSceneAnswer, or what a test sets per
// command, and the ask side answers TConsoleSceneConfirmed. Every call lands in TConsoleSceneCalls in order, with the
// name and target it carried; the load runs both questions the engine would ask.
internal sealed class TConsoleScenePort : LConsoleScenePort, CEnvoy
{
    public LConsoleSceneAnswer TConsoleSceneAnswer { get; set; } = new("Night", true, false, null);

    public LConsoleSceneAnswer? TConsoleSceneOptional { get; set; }

    public string? TConsoleSceneFault { get; set; }

    public bool TConsoleSceneConfirmed { get; set; }

    public List<string> TConsoleSceneCalls { get; } = [];

    public List<object?[]> TConsoleSceneAsks { get; } = [];

    public List<LConsoleWindow?> TConsoleSceneWindows { get; } = [];

    public LConsoleSceneAnswer LConsoleSceneStart(LConsoleWindow? lWindow) =>
        TConsoleSceneRecord(lWindow, "start");

    public LConsoleSceneAnswer LConsoleSceneRebuild(string lName, LConsoleWindow? lWindow) =>
        TConsoleSceneRecord(lWindow, "rebuild", lName);

    public LConsoleSceneAnswer LConsoleSceneRead(string lName, LConsoleWindow? lWindow) =>
        TConsoleSceneRecord(lWindow, "read", lName);

    public LConsoleSceneAnswer LConsoleSceneLoad(
        string lName, string lTarget, LConsoleWindow? lWindow, Func<bool> lLoadConfirm, Func<bool> lCloseConfirm)
    {
        TConsoleSceneRecord(lWindow, "load", lName, lTarget);
        TConsoleSceneCalls.Add("answer " + lLoadConfirm() + " " + lCloseConfirm());
        return TConsoleSceneAnswer;
    }

    public LConsoleSceneAnswer? LConsoleSceneDelete(string lName, string lTarget, LConsoleWindow? lWindow)
    {
        TConsoleSceneRecord(lWindow, "delete", lName, lTarget);
        return TConsoleSceneOptional;
    }

    public LConsoleSceneAnswer? LConsoleSceneSave(string lName, LConsoleWindow? lWindow)
    {
        TConsoleSceneRecord(lWindow, "save", lName);
        return TConsoleSceneOptional;
    }

    public string LConsoleSceneResolve(string lName, string lFallback, LConsoleWindow? lWindow)
    {
        TConsoleSceneRecord(lWindow, "resolve", lName, lFallback);
        return lName + ".json";
    }

    public string? LConsoleScenePersist(string lName, string lPath, string lFallback, LConsoleWindow? lWindow)
    {
        TConsoleSceneRecord(lWindow, "write", lName, lPath, lFallback);
        return TConsoleSceneFault;
    }

    public (LConsoleSceneAnswer? LConsoleSceneImported, string? LConsoleSceneFault) LConsoleSceneImport(
        string lName, string lPath, string lFallback, LConsoleWindow? lWindow)
    {
        TConsoleSceneRecord(lWindow, "import", lName, lPath, lFallback);
        return (TConsoleSceneOptional, TConsoleSceneFault);
    }

    public bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey) =>
        TConsoleSceneConfirm(questionKey, actionKey, titleKey);

    public bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey, object? argument) =>
        TConsoleSceneConfirm(questionKey, actionKey, titleKey, argument);

    public bool CEnvoyConfirm(
        string questionKey, string subjectKey, object? subject, string actionKey, string titleKey, string dismissKey) =>
        TConsoleSceneConfirm(questionKey, subjectKey, subject, actionKey, titleKey, dismissKey);

    public bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey) =>
        TConsoleSceneConfirm("destructive", questionKey, actionKey, titleKey);

    public bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey, object? argument) =>
        TConsoleSceneConfirm("destructive", questionKey, actionKey, titleKey, argument);

    public void CEnvoyWarningShow(string titleKey, string messageKey) =>
        TConsoleSceneConfirm("warning", titleKey, messageKey);

    public void CEnvoyFailureShow(string titleKey, string messageKey) =>
        TConsoleSceneConfirm("failure", titleKey, messageKey);

    private LConsoleSceneAnswer TConsoleSceneRecord(LConsoleWindow? window, params string[] call)
    {
        TConsoleSceneCalls.Add(string.Join(" ", call));
        TConsoleSceneWindows.Add(window);
        return TConsoleSceneAnswer;
    }

    private bool TConsoleSceneConfirm(params object?[] ask)
    {
        TConsoleSceneAsks.Add(ask);
        return TConsoleSceneConfirmed;
    }
}
