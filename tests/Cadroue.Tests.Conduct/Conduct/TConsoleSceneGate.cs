using System;
using System.Collections.Generic;
using Cadroue.Conduct;
using Cadroue.ShellEngine;
using Xunit;

namespace Cadroue.Tests;

public sealed class TConsoleSceneGate
{
    private readonly TConsoleScenePort tConsoleScenePort = new();
    private readonly CConsoleScene tConsoleSceneGate;
    private readonly List<string> tConsoleSceneNotices = [];
    private bool tConsoleSceneBusy;

    public TConsoleSceneGate()
    {
        tConsoleSceneGate = TAtelier.TAtelierCreate(port => port with { LAtelierScene = tConsoleScenePort })
            .CAtelierConsoleCreate()
            .CConsoleScene;
        tConsoleSceneGate.CConsoleSceneListed += () => tConsoleSceneNotices.Add("listed");
        tConsoleSceneGate.CConsoleSceneChanged += () => tConsoleSceneNotices.Add("changed");
        tConsoleSceneGate.CConsoleSceneMarked += () => tConsoleSceneNotices.Add("marked");
    }

    private void TConsoleSceneAttach() =>
        tConsoleSceneGate.CConsoleSceneAttach(
            _ => throw new InvalidOperationException(),
            (_, _, _) => throw new InvalidOperationException(),
            () => { },
            () => tConsoleSceneBusy);

    private static void TConsoleSceneCheck(string key, object[]? arguments, CConsoleText? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(key, actual.CConsoleTextKey);
        Assert.Equal(arguments is null, actual.CConsoleTextArguments is null);
        Assert.Equal(arguments ?? [], actual.CConsoleTextArguments ?? []);
    }

    [Fact]
    public void Start_TakesTheEngineState_AndTellsTheChange()
    {
        tConsoleScenePort.TConsoleSceneAnswer = new("Night", true, true, null);
        tConsoleSceneGate.CConsoleSceneStart();

        Assert.Equal(["start"], tConsoleScenePort.TConsoleSceneCalls);
        Assert.Equal("Night", tConsoleSceneGate.CConsoleSceneName);
        Assert.Equal("Night", tConsoleSceneGate.CConsoleSceneSelected);
        Assert.True(tConsoleSceneGate.CConsoleSceneDirty);
        Assert.Equal(["changed"], tConsoleSceneNotices);
        Assert.Equal("json", tConsoleSceneGate.CConsoleSceneFile.CConsoleSceneExtension);
    }

    [Fact]
    public void Rebuild_TellsTheNamesBeforeTheChange_AndAnUnstoredSceneSelectsNothing()
    {
        tConsoleScenePort.TConsoleSceneAnswer = new("Day", false, false, ["Night"]);
        tConsoleSceneGate.CConsoleSceneRebuild();

        Assert.Equal(["rebuild "], tConsoleScenePort.TConsoleSceneCalls);
        Assert.Equal(["Night"], tConsoleSceneGate.CConsoleSceneNames);
        Assert.Null(tConsoleSceneGate.CConsoleSceneSelected);
        Assert.Equal(["listed", "changed"], tConsoleSceneNotices);
    }

    [Fact]
    public void Tick_ChecksTheMark_OnlyWhileNamedAndUnfocused()
    {
        tConsoleSceneGate.CConsoleSceneTick(false);
        Assert.Empty(tConsoleScenePort.TConsoleSceneCalls);

        tConsoleSceneGate.CConsoleSceneStart();
        tConsoleSceneGate.CConsoleSceneTick(true);
        tConsoleScenePort.TConsoleSceneAnswer = new("Night", true, true, null);
        tConsoleSceneGate.CConsoleSceneTick(false);

        Assert.Equal(["start", "read Night"], tConsoleScenePort.TConsoleSceneCalls);
        Assert.True(tConsoleSceneGate.CConsoleSceneDirty);
        Assert.Equal(["changed", "marked"], tConsoleSceneNotices);
    }

    [Fact]
    public void Reload_IsHeldByARow_AndConsumedOnceByTheClose()
    {
        tConsoleSceneGate.CConsoleSceneAccept("Day");
        tConsoleSceneGate.CConsoleSceneAccept(null);
        Assert.Equal("Day", tConsoleSceneGate.CConsoleSceneReload);

        tConsoleSceneGate.CConsoleSceneClose(tConsoleScenePort);
        tConsoleSceneGate.CConsoleSceneClose(tConsoleScenePort);

        Assert.Null(tConsoleSceneGate.CConsoleSceneReload);
        Assert.Equal(["load  Day", "answer False True", "read Night"], tConsoleScenePort.TConsoleSceneCalls);
        tConsoleSceneGate.CConsoleSceneSet("Day");
        tConsoleSceneGate.CConsoleSceneSet(null);
        Assert.Null(tConsoleSceneGate.CConsoleSceneReload);
    }

    [Fact]
    public void Select_LoadsAnotherScene_OrHoldsItWhileTheListIsOpen()
    {
        tConsoleSceneGate.CConsoleSceneStart();
        tConsoleSceneGate.CConsoleSceneSelect("night", false, tConsoleScenePort);
        tConsoleSceneGate.CConsoleSceneSelect(null, false, tConsoleScenePort);
        tConsoleSceneGate.CConsoleSceneSelect("Day", true, tConsoleScenePort);
        Assert.Equal("Day", tConsoleSceneGate.CConsoleSceneReload);

        tConsoleSceneGate.CConsoleSceneSelect("Day", false, tConsoleScenePort);

        Assert.Null(tConsoleSceneGate.CConsoleSceneReload);
        Assert.Equal(["start", "load Night Day", "answer False True"], tConsoleScenePort.TConsoleSceneCalls);
    }

    [Fact]
    public void Load_AsksTheBusyQuestion_OnlyWhileATabWorks()
    {
        TConsoleSceneAttach();
        tConsoleScenePort.TConsoleSceneConfirmed = true;
        tConsoleSceneGate.CConsoleSceneSelect("Day", false, tConsoleScenePort);
        tConsoleSceneBusy = true;
        tConsoleSceneGate.CConsoleSceneSelect("Day", false, tConsoleScenePort);

        object?[] load = ["Console.Scene.LoadConfirm", "Terms.Load", "Console.Scene.LoadTitle", "Day"];
        object?[] busy = ["Tab.Close.BusyAllMessage", "Terms.Stop", "Tab.Close.BusyTitle"];
        Assert.Equal(new[] { load, load, busy }, tConsoleScenePort.TConsoleSceneAsks);
        Assert.All(tConsoleScenePort.TConsoleSceneWindows, Assert.NotNull);
    }

    [Fact]
    public void Delete_IsNotHandledWithoutARow_AndDropsTheReload()
    {
        Assert.False(tConsoleSceneGate.CConsoleSceneDelete(null));
        Assert.Empty(tConsoleScenePort.TConsoleSceneCalls);

        tConsoleSceneGate.CConsoleSceneAccept("Day");
        Assert.True(tConsoleSceneGate.CConsoleSceneDelete("Day"));
        Assert.Null(tConsoleSceneGate.CConsoleSceneReload);
        Assert.Empty(tConsoleSceneNotices);

        tConsoleScenePort.TConsoleSceneOptional = new(string.Empty, false, false, []);
        Assert.True(tConsoleSceneGate.CConsoleSceneDelete("Day"));
        Assert.Equal(["delete  Day", "delete  Day"], tConsoleScenePort.TConsoleSceneCalls);
        Assert.Equal(["listed", "changed"], tConsoleSceneNotices);
    }

    [Fact]
    public void Save_RefusesABlankName_AndTrimsTheText()
    {
        Assert.False(tConsoleSceneGate.CConsoleSceneSave("   "));
        Assert.False(tConsoleSceneGate.CConsoleSceneSave(null));
        Assert.Empty(tConsoleScenePort.TConsoleSceneCalls);

        tConsoleScenePort.TConsoleSceneOptional = new("Day", true, false, ["Day"]);
        Assert.True(tConsoleSceneGate.CConsoleSceneSave("  Day "));
        Assert.Equal(["save Day"], tConsoleScenePort.TConsoleSceneCalls);
        Assert.Equal("Day", tConsoleSceneGate.CConsoleSceneSelected);
        Assert.Equal("Day.json", tConsoleSceneGate.CConsoleSceneFile.CConsoleSceneResolve(" Day ", "Scene"));
    }

    [Fact]
    public void Write_WordsTheFault_OnlyAfterAConfirmedDialog()
    {
        CConsoleFile file = tConsoleSceneGate.CConsoleSceneFile;
        Assert.Null(file.CConsoleScenePersist(false, "a.json", "Day", "Scene"));
        Assert.Null(file.CConsoleScenePersist(null, "a.json", "Day", "Scene"));
        Assert.Empty(tConsoleScenePort.TConsoleSceneCalls);

        Assert.Null(file.CConsoleScenePersist(true, "a.json", " Day ", "Scene"));
        tConsoleScenePort.TConsoleSceneFault = "disk";
        TConsoleSceneCheck(
            "Console.Scene.Error.Write", ["disk"], file.CConsoleScenePersist(true, "a.json", null, "S"));
        tConsoleScenePort.TConsoleSceneFault = string.Empty;
        TConsoleSceneCheck(
            "Console.Scene.Error.Write", null, file.CConsoleScenePersist(true, "a.json", null, "S"));
        Assert.Equal("write Day a.json Scene", tConsoleScenePort.TConsoleSceneCalls[0]);
    }

    [Fact]
    public void Import_WordsTheReadFault_OrTheInvalidFile_OrAppliesTheScene()
    {
        CConsoleFile file = tConsoleSceneGate.CConsoleSceneFile;
        Assert.Null(file.CConsoleSceneImport(false, "a.json", "Imported"));
        Assert.Empty(tConsoleScenePort.TConsoleSceneCalls);

        TConsoleSceneCheck(
            "Console.Scene.Error.Invalid", null, file.CConsoleSceneImport(true, "a.json", "Imported"));
        tConsoleScenePort.TConsoleSceneFault = "locked";
        TConsoleSceneCheck(
            "Console.Scene.Error.Read", ["locked"], file.CConsoleSceneImport(true, "a.json", "Imported"));
        tConsoleScenePort.TConsoleSceneFault = null;
        tConsoleScenePort.TConsoleSceneOptional = new(string.Empty, false, false, ["Night"]);

        Assert.Null(file.CConsoleSceneImport(true, "a.json", "Imported"));
        Assert.Equal(["Night"], tConsoleSceneGate.CConsoleSceneNames);
        Assert.Equal("import  a.json Imported", tConsoleScenePort.TConsoleSceneCalls[0]);
    }

    [Fact]
    public void Attach_RefusesAMissingCallable()
    {
        Assert.Throws<ArgumentNullException>(
            () => tConsoleSceneGate.CConsoleSceneAttach(null!, (_, _, _) => false, () => { }, () => false));
        Assert.Throws<ArgumentNullException>(
            () => TAtelier.TAtelierCreate(port => port with { LAtelierScene = null! }));
    }
}
