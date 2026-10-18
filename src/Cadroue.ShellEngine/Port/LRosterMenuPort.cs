namespace Cadroue.ShellEngine;

public interface LRosterMenuPort
{
    (bool LRosterMenuRestartable, bool LRosterMenuCancellable, int LRosterMenuCount,
        IReadOnlyList<string> LRosterMenuPaths, Action LRosterMenuAction) LRosterMenuRead(
        LStation lStation, Guid lClicked);
}
