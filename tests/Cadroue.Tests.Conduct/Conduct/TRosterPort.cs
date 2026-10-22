using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TRosterPort : LRosterPort, LRosterMenuPort, LAboutPort
{
    public bool TRosterShared { get; set; }

    public bool TRosterDone { get; set; }

    public bool TRosterDeveloper { get; set; }

    public bool LAboutDeveloperRead() => TRosterDeveloper;

    public void LAboutDeveloperSave(bool lDeveloper) => TRosterDeveloper = lDeveloper;

    public List<string> TRosterCalls { get; } = [];

    public bool LRosterSharedRead() => TRosterShared;

    public bool LRosterDoneRead() => TRosterDone;

    public bool LRosterSharedSave(bool lShared)
    {
        TRosterCalls.Add($"shared:{lShared}");
        if (lShared == TRosterShared)
        {
            return false;
        }

        TRosterShared = lShared;
        return true;
    }

    public bool LRosterDoneSave(bool lDone)
    {
        TRosterCalls.Add($"done:{lDone}");
        if (lDone == TRosterDone)
        {
            return false;
        }

        TRosterDone = lDone;
        return true;
    }

    public LStation? TRosterStation { get; private set; }

    public (bool, bool, int, IReadOnlyList<string>, Action) TRosterMenu { get; set; } =
        (false, false, 0, [], () => { });

    public (bool LRosterMenuRestartable, bool LRosterMenuCancellable, int LRosterMenuCount,
        IReadOnlyList<string> LRosterMenuPaths, Action LRosterMenuAction) LRosterMenuRead(
        LStation lStation, Guid lClicked)
    {
        TRosterCalls.Add($"menu:{lClicked}");
        TRosterStation = lStation;
        return TRosterMenu;
    }
}
