using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests.Interface;

internal sealed class TScheduleSignet : IDisposable
{
    private readonly string tScheduleRoot;
    private readonly string tSignetDepotRoot;
    private readonly LPreferenceState tSignetPreference;
    private readonly LSchedule tSchedule;
    private int tScheduleSequence;

    internal TScheduleSignet()
    {
        tScheduleRoot = Path.Combine(
            Path.GetTempPath(),
            "cadroue-signet-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tScheduleRoot);
        tSignetDepotRoot = LDepot.LDepotRootRead();
        tSignetPreference = LPreference.LPreferenceStateCurrent;
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tScheduleRoot);
        tSchedule = new LSchedule();
        tSchedule.LScheduleLoad();
    }

    internal void TSignetSet(Guid signet) => LSignet.LSignetSource = () => signet;

    internal void TSignetSharedSet(bool shared)
    {
        LPreferenceState next = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        next.LPreferenceWorklistShared = shared;
        LPreference.LPreferenceStateSet(next);
    }

    internal Guid TWorkCreate(string name)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        var workItem = new LWorkItem(
            Guid.NewGuid(),
            LWorkKind.LWorkKindEdit,
            LWorkPriority.LWorkPriorityNormal,
            Path.Combine(tScheduleRoot, name + ".source"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created);
        new LScheduleIntake(tSchedule).LScheduleAcceptedAdd(new[] { workItem });
        return workItem.LWorkId;
    }

    internal Guid TRelayWorkCreate(string name, int relayProcess)
    {
        DateTimeOffset created = DateTimeOffset.UnixEpoch.AddTicks(++tScheduleSequence);
        var workItem = new LWorkItem(
            Guid.NewGuid(),
            LWorkKind.LWorkKindAudio,
            LWorkPriority.LWorkPriorityNormal,
            Path.Combine(tScheduleRoot, name + ".source"),
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1),
            name,
            Path.Combine(tScheduleRoot, name + ".output"),
            TWorkOutput.TWorkOutputCreate(),
            lWorkCreateTime: created);
        new LScheduleIntake(tSchedule).LScheduleAcceptedAdd(new[] { workItem }, Guid.NewGuid());
        string filePath = LDepot.LDepotFileRead(LDepotFolder.LDepotFolderScheduled, workItem.LWorkId);
        LWorkDocument record = LScheduleStore.LScheduleRecordRead(filePath)!;
        record.LWorkRelayProcess = relayProcess;
        record.LWorkRelayStamp = 0;
        LScheduleStore.LScheduleRecordSave(record, LDepotFolder.LDepotFolderScheduled);
        return workItem.LWorkId;
    }

    internal bool TSignetClaimCheck() => new LScheduleLease(tSchedule).LScheduleClaim(Guid.NewGuid()) is not null;

    internal Guid? TSignetClaimRead() => new LScheduleLease(tSchedule).LScheduleClaim(Guid.NewGuid())?.LWorkId;

    internal IReadOnlyList<Guid> TSignetDisplayRead()
    {
        tSchedule.LScheduleLoad();
        return tSchedule.LScheduleRecords.Select(item => item.LWorkId).ToArray();
    }

    public void Dispose()
    {
        LSignet.LSignetSource = null;
        new LScheduleRemover(tSchedule).LScheduleAllClear();
        LPreference.LPreferenceStateSet(tSignetPreference);
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tSignetDepotRoot);
        try
        {
            Directory.Delete(tScheduleRoot, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
