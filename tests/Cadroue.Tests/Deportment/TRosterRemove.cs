using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

[Collection("Schedule")]
public sealed class TRosterRemove
{
    private static LRoster TRosterOpen(TSchedule schedule)
    {
        LRoster roster = TInterface.TRosterCreate(
            schedule.TScheduleRead(), TInterface.TStationCreate(schedule.TScheduleRead()));
        TInterface.TRosterRebuild(roster);
        return roster;
    }

    private static LPreferenceState TPreferenceConfirmSet(bool confirm)
    {
        LPreferenceState before = LPreference.LPreferenceStateCurrent;
        LPreferenceState draft = TInterface.TPreferenceClone(before);
        draft.LPreferenceConfirmDestructive = confirm;
        TInterface.TPreferenceRestore(draft);
        return before;
    }

    [Fact]
    public void Remove_WithoutConfirmPreference_RunsAtOnce()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(batch, "one"), schedule.TWorkCreate(Guid.NewGuid(), "two"));
        LRoster roster = TRosterOpen(schedule);
        LPreferenceState before = TPreferenceConfirmSet(false);
        int asked = 0;
        void TAskHandle(LAsk ask, Action<bool> answer) => asked++;
        TInterface.TAskAttach(TAskHandle);
        try
        {
            TInterface.TRosterRemove(roster, batch);
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
            TInterface.TPreferenceRestore(before);
        }

        Assert.Equal(0, asked);
        TScheduleItem left = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.NotEqual(batch, left.TScheduleBatchId);
        Assert.Single(roster.LRosterCards);
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Remove_WithConfirmPreference_AsksAndHonoursTheAnswer()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(batch, "one"), schedule.TWorkCreate(batch, "two"));
        LRoster roster = TRosterOpen(schedule);
        LPreferenceState before = TPreferenceConfirmSet(true);
        var asks = new List<LAsk>();
        bool reply = false;
        void TAskHandle(LAsk ask, Action<bool> answer)
        {
            asks.Add(ask);
            answer(reply);
        }

        TInterface.TAskAttach(TAskHandle);
        try
        {
            TInterface.TRosterRemove(roster, batch);
            Assert.Equal(2, schedule.TScheduleRecordsRead().Count);
            reply = true;
            TInterface.TRosterRemove(roster, batch);
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
            TInterface.TPreferenceRestore(before);
        }

        Assert.Equal(2, asks.Count);
        Assert.NotEmpty(asks[0].LAskQuestion);
        Assert.NotEmpty(asks[0].LAskTitle);
        Assert.Empty(schedule.TScheduleRecordsRead());
        TInterface.TRosterClose(roster);
    }

    [Fact]
    public void Remove_UnknownBatch_NeitherAsksNorWarns()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        LRoster roster = TRosterOpen(schedule);
        LPreferenceState before = TPreferenceConfirmSet(true);
        int asked = 0;
        int warned = 0;
        void TAskHandle(LAsk ask, Action<bool> answer) => asked++;
        TInterface.TAskAttach(TAskHandle);
        TInterface.TRosterWarningAttach(roster, (_, _) => warned++);
        try
        {
            TInterface.TRosterRemove(roster, Guid.NewGuid());
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
            TInterface.TPreferenceRestore(before);
        }

        Assert.Equal(0, asked);
        Assert.Equal(0, warned);
        Assert.Single(schedule.TScheduleRecordsRead());
        TInterface.TRosterClose(roster);
    }
}
