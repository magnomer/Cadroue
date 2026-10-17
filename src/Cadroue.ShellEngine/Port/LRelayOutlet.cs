using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The engine's face for the relay gate: every member forwards to the same-named member of the engine's relay facade.
// It rejects a null engine and keeps it for every forward.
public sealed class LRelayOutlet : LRelayPort
{
    private readonly LEngine _lRelayOutletEngine;

    public LRelayOutlet(LEngine lEngine)
    {
        ArgumentNullException.ThrowIfNull(lEngine);
        _lRelayOutletEngine = lEngine;
    }

    public void LRelayAttach(
        Func<Guid, string, Guid, bool> lIntake,
        Func<Guid, string, Guid, bool> lTrack,
        Action<Guid, IReadOnlyList<string>, bool> lDrop,
        Action<Guid, string, Guid> lArrive,
        Action<IReadOnlyList<Guid>> lEvict,
        Action<IReadOnlyList<(string LRelayPath, Guid LRelayBatch, Guid LRelayTab)>> lUnlock,
        Func<IReadOnlyList<(Guid LRelayTab, IReadOnlyList<(string, Guid)> LRelayLocks)>, bool> lClaim,
        Func<Guid, string, Guid, bool> lDeliver) =>
        _lRelayOutletEngine.LEngineRelay.LRelayAttach(
            lIntake, lTrack, lDrop, lArrive, lEvict, lUnlock, lClaim, lDeliver);

    public bool LRelayEmptyCheck() => _lRelayOutletEngine.LEngineRelay.LRelayEmptyCheck();

    public int LRelayPathsRemove(LDocket lDocket, IReadOnlyList<string> lPaths) =>
        _lRelayOutletEngine.LEngineRelay.LRelayPathsRemove(lDocket, lPaths);

    public int LRelayRelease(LDocket lDocket, IReadOnlyList<(string, Guid)> lReleases) =>
        _lRelayOutletEngine.LEngineRelay.LRelayRelease(lDocket, lReleases);

    public bool LRelayClaim(LDocket lDocket, IReadOnlyList<(string, Guid)> lLocks) =>
        _lRelayOutletEngine.LEngineRelay.LRelayClaim(lDocket, lLocks);

    public (IReadOnlyList<(string, Guid)> LRelayOrphans, IReadOnlyList<string> LRelayDelivered) LRelayDepartedRead(
        LDocket lDocket, IReadOnlySet<Guid> lBatches) =>
        _lRelayOutletEngine.LEngineRelay.LRelayDepartedRead(lDocket, lBatches);

    public (int LRelayScanned, bool LRelayListed, int LRelayAdded) LRelayDeliveredAdd(
        LDocket lDocket, string lPath, Guid lBatch, bool lLocked) =>
        _lRelayOutletEngine.LEngineRelay.LRelayDeliveredAdd(lDocket, lPath, lBatch, lLocked);

    public void LRelayInfoRecord(string lMessage) => _lRelayOutletEngine.LEngineRelay.LRelayInfoRecord(lMessage);

    public void LRelayWarningRecord(string lMessage) => _lRelayOutletEngine.LEngineRelay.LRelayWarningRecord(lMessage);
}
