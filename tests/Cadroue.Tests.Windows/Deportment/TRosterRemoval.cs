using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Schedule")]
public sealed class TRosterRemoval
{
    private static QRoster TRosterOpen(TSchedule schedule)
    {
        QRoster roster = TInterfaceRoster.TRosterCreate(
            schedule.TScheduleRead(), TInterfaceRoster.TStationCreate(schedule.TScheduleRead()));
        TInterfaceRoster.TRosterRebuild(roster);
        return roster;
    }

    private static LPreferenceState TPreferenceConfirmSet(bool confirm)
    {
        LPreferenceState before = LPreference.LPreferenceStateCurrent;
        LPreferenceState draft = Interface.TInterface.TPreferenceClone(before);
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
        QRoster roster = TRosterOpen(schedule);
        LPreferenceState before = TPreferenceConfirmSet(false);
        int asked = 0;
        void TAskHandle(QAsk ask, Action<bool> answer) => asked++;
        TInterface.TAskAttach(TAskHandle);
        try
        {
            TInterfaceRoster.TRosterRemove(roster, batch);
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
            TInterface.TPreferenceRestore(before);
        }

        Assert.Equal(0, asked);
        TScheduleItem left = Assert.Single(schedule.TScheduleRecordsRead());
        Assert.NotEqual(batch, left.TScheduleBatchId);
        Assert.Single(TInterfaceRoster.TRosterCardsRead(roster));
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Remove_WithConfirmPreference_AsksAndHonoursTheAnswer()
    {
        using var schedule = new TSchedule();
        Guid batch = Guid.NewGuid();
        schedule.TScheduleAdd(schedule.TWorkCreate(batch, "one"), schedule.TWorkCreate(batch, "two"));
        QRoster roster = TRosterOpen(schedule);
        LPreferenceState before = TPreferenceConfirmSet(true);
        var asks = new List<QAsk>();
        bool reply = false;
        void TAskHandle(QAsk ask, Action<bool> answer)
        {
            asks.Add(ask);
            answer(reply);
        }

        TInterface.TAskAttach(TAskHandle);
        try
        {
            TInterfaceRoster.TRosterRemove(roster, batch);
            Assert.Equal(2, schedule.TScheduleRecordsRead().Count);
            reply = true;
            TInterfaceRoster.TRosterRemove(roster, batch);
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
            TInterface.TPreferenceRestore(before);
        }

        Assert.Equal(2, asks.Count);
        Assert.NotEmpty(asks[0].QAskQuestion);
        Assert.NotEmpty(asks[0].QAskTitle);
        Assert.Empty(schedule.TScheduleRecordsRead());
        TInterfaceRoster.TRosterClose(roster);
    }

    [Fact]
    public void Remove_UnknownBatch_NeitherAsksNorWarns()
    {
        using var schedule = new TSchedule();
        schedule.TScheduleAdd(schedule.TWorkCreate(Guid.NewGuid(), "one"));
        QRoster roster = TRosterOpen(schedule);
        LPreferenceState before = TPreferenceConfirmSet(true);
        int asked = 0;
        int warned = 0;
        void TAskHandle(QAsk ask, Action<bool> answer) => asked++;
        TInterface.TAskAttach(TAskHandle);
        TInterfaceRoster.TRosterWarningAttach(roster, (_, _) => warned++);
        try
        {
            TInterfaceRoster.TRosterRemove(roster, Guid.NewGuid());
        }
        finally
        {
            TInterface.TAskDetach(TAskHandle);
            TInterface.TPreferenceRestore(before);
        }

        Assert.Equal(0, asked);
        Assert.Equal(0, warned);
        Assert.Single(schedule.TScheduleRecordsRead());
        TInterfaceRoster.TRosterClose(roster);
    }
}
