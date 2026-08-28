using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.Tests;

internal sealed class TScheduleSignet : IDisposable
{
    private readonly string tScheduleRoot;
    private readonly string tPreviousDepotRoot;
    private readonly LPreferenceState tPreviousPreference;
    private readonly LSchedule tSchedule;
    private int tScheduleSequence;

    internal TScheduleSignet()
    {
        tScheduleRoot = Path.Combine(
            Path.GetTempPath(),
            "cadroue-signet-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tScheduleRoot);
        tPreviousDepotRoot = LDepot.LDepotRootRead();
        tPreviousPreference = LPreference.LPreferenceStateCurrent;
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tScheduleRoot);
        tSchedule = new LSchedule();
        tSchedule.LScheduleLoad();
    }

    internal void SignetSet(Guid signet) => LSignet.LSignetSource = () => signet;

    internal void SharedSet(bool shared)
    {
        LPreferenceState next = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        next.LPreferenceWorklistShared = shared;
        LPreference.LPreferenceStateSet(next);
    }

    internal Guid WorkCreate(string name)
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
            WorkCreationOutput.Create(),
            lWorkCreateTime: created);
        tSchedule.LScheduleAdd(new[] { workItem });
        return workItem.LWorkId;
    }

    internal bool ClaimFound() => tSchedule.LScheduleClaim(Guid.NewGuid()) is not null;

    internal Guid? ClaimId() => tSchedule.LScheduleClaim(Guid.NewGuid())?.LWorkId;

    internal IReadOnlyList<Guid> DisplayedRead()
    {
        tSchedule.LScheduleLoad();
        return tSchedule.LScheduleRecords.Select(item => item.LWorkId).ToArray();
    }

    public void Dispose()
    {
        LSignet.LSignetSource = null;
        tSchedule.LScheduleAllClear();
        LPreference.LPreferenceStateSet(tPreviousPreference);
        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tPreviousDepotRoot);
        try
        {
            Directory.Delete(tScheduleRoot, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
