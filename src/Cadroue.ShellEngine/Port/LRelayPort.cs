using Cadroue.Application;

namespace Cadroue.ShellEngine;

// The relay gate's reach into the engine: the delivery seams it answers, the relay preference, each docket edit it
// asks for with the docket passed through unread, and the traces it leaves.
public interface LRelayPort
{
    // The cartographer's and the messenger's delivery seams now reach these callables. Each work item arrives as the
    // tab it came from (Guid.Empty for none) and the paths it holds.
    void LRelayAttach(
        Func<Guid, string, Guid, bool> lIntake,
        Func<Guid, string, Guid, bool> lTrack,
        Action<Guid, IReadOnlyList<string>, bool> lDrop,
        Action<Guid, string, Guid> lArrive,
        Action<IReadOnlyList<Guid>> lEvict,
        Action<IReadOnlyList<(string LRelayPath, Guid LRelayBatch, Guid LRelayTab)>> lUnlock,
        Func<IReadOnlyList<(Guid LRelayTab, IReadOnlyList<(string, Guid)> LRelayLocks)>, bool> lClaim,
        Func<Guid, string, Guid, bool> lDeliver);

    // Whether the relay empties its sources from their tab after delivery.
    bool LRelayEmptyCheck();

    // Answers the count removed.
    int LRelayPathsRemove(LDocket lDocket, IReadOnlyList<string> lPaths);

    // Answers the count unlocked.
    int LRelayRelease(LDocket lDocket, IReadOnlyList<(string, Guid)> lReleases);

    // False when another batch already holds one of the paths.
    bool LRelayClaim(LDocket lDocket, IReadOnlyList<(string, Guid)> lLocks);

    // Of the entries the batches put on the docket: the locked ones left undelivered, and the delivered paths.
    (IReadOnlyList<(string, Guid)> LRelayOrphans, IReadOnlyList<string> LRelayDelivered) LRelayDepartedRead(
        LDocket lDocket, IReadOnlySet<Guid> lBatches);

    // Scans the path for media and adds what it finds as delivered: the count scanned (nothing is added when 0),
    // whether the docket listed the path before, and the count added.
    (int LRelayScanned, bool LRelayListed, int LRelayAdded) LRelayDeliveredAdd(
        LDocket lDocket, string lPath, Guid lBatch, bool lLocked);

    void LRelayInfoRecord(string lMessage);

    void LRelayWarningRecord(string lMessage);
}
