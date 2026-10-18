namespace Cadroue.ShellEngine;

public interface LRosterPort
{
    bool LRosterSharedRead();

    bool LRosterDoneRead();

    bool LRosterSharedSave(bool lShared);

    bool LRosterDoneSave(bool lDone);
}
