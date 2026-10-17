namespace Cadroue.ShellEngine;

public interface LRosterPort
{
    bool LRosterSharedRead();

    bool LRosterDoneRead();

    bool LRosterSharedSave(bool lShared);

    bool LRosterDoneSave(bool lDone);

    (bool LRosterMenuRestartable, bool LRosterMenuCancellable, int LRosterMenuCount,
        IReadOnlyList<string> LRosterMenuPaths, Action LRosterMenuAction) LRosterMenuRead(
        LStation lStation, Guid lClicked);
}
