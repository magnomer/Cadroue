using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TFlowKeyframeTick
{
    private static LFlow TFlowBuild(bool command = true)
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, command);
        TInterface.TFlowSourceSet(
            flow, TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360), "C:\\media\\clip.mp4");
        return flow;
    }

    private static List<string> TFlowTimerAttach(LFlow flow)
    {
        List<string> timers = [];
        TInterface.TFlowTimerAttach(
            flow,
            () => timers.Add("defer"),
            delay => timers.Add("resume"),
            () => timers.Add("reset"));
        return timers;
    }

    [Fact]
    public void Defer_StartsTheRequestTimer_OnlyWithASourceAndNoResumePending()
    {
        LFlow flow = TFlowBuild();
        List<string> timers = TFlowTimerAttach(flow);

        TInterface.TFlowKeyframeDefer(flow);
        Assert.Equal(["defer"], timers);

        timers.Clear();
        TInterface.TFlowKeyframeSuspend(flow, true);
        Assert.True(TInterface.TFlowResumeCheck(flow));
        Assert.Equal(["reset", "resume"], timers);

        timers.Clear();
        TInterface.TFlowKeyframeDefer(flow);
        Assert.Empty(timers);

        TInterface.TFlowKeyframeTick(flow);
        Assert.False(TInterface.TFlowResumeCheck(flow));
        Assert.Equal(["reset"], timers);

        timers.Clear();
        TInterface.TFlowSourceClear(flow);
        TInterface.TFlowKeyframeDefer(flow);
        Assert.Equal(["reset"], timers);
    }

    [Fact]
    public void Suspend_WithoutCommand_NeverArmsTheResume()
    {
        LFlow flow = TFlowBuild(false);
        List<string> timers = TFlowTimerAttach(flow);
        TInterface.TFlowKeyframeSuspend(flow, true);
        Assert.False(TInterface.TFlowResumeCheck(flow));
        Assert.Equal(["reset"], timers);
    }

    [Fact]
    public void CursorUpdate_Defers_AndSeekSuspends()
    {
        LFlow flow = TFlowBuild();
        List<string> timers = TFlowTimerAttach(flow);
        List<TimeSpan> seeks = [];
        int applies = 0;
        TInterface.TFlowCursorAttach(flow, () => applies++, seeks.Add);

        TInterface.TFlowCursorUpdate(flow, TimeSpan.FromSeconds(5));
        Assert.Equal(["defer"], timers);
        Assert.Equal(1, applies);
        Assert.Empty(seeks);

        timers.Clear();
        TInterface.TFlowCursorSeek(flow, TimeSpan.FromSeconds(90));
        Assert.Equal(["reset", "resume"], timers);
        Assert.Equal([TimeSpan.FromSeconds(60)], seeks);
        Assert.Equal(2, applies);

        TInterface.TFlowCommandSet(flow, false);
        TInterface.TFlowCursorUpdate(flow, TimeSpan.FromSeconds(1));
        Assert.Equal(2, applies);
    }

    [Fact]
    public void Apply_IgnoresStaleSerials_AndForwardsTheCurrentOne()
    {
        LFlow flow = TFlowBuild();
        List<int> counts = [];
        TInterface.TFlowKeyframeAttach(flow, notice => { }, (entries, ranges) => counts.Add(entries.Count));
        LKeyframeEntry[] entries = [new(TimeSpan.FromSeconds(2))];
        LKeyframeScanRange[] ranges = [new(TimeSpan.Zero, TimeSpan.FromSeconds(10))];

        TInterface.TFlowKeyframeApply(
            flow, TInterface.TKeyframeNoticeCreate(7, entries, ranges, LKeyframeKind.LKeyframeKindInter));
        Assert.Empty(counts);

        TInterface.TFlowKeyframeApply(
            flow, TInterface.TKeyframeNoticeCreate(0, entries, ranges, LKeyframeKind.LKeyframeKindInter));
        Assert.Equal([1], counts);
        Assert.Equal("LKeyframeKindInter/1/1/10", flow.LFlowKeyframeStamp);
    }

    [Fact]
    public void Shortcut_RunsOnlyWithCommandAndSpool()
    {
        LFlow flow = TFlowBuild();
        int spools = 0;
        TInterface.TFlowSpoolAttach(flow, () => spools++);
        Assert.True(TInterface.TFlowShortcutRun(flow, "ZoomIn"));
        Assert.Equal(1, spools);
        Assert.False(TInterface.TFlowShortcutRun(flow, "SectionAdd"));

        TInterface.TFlowSectionSet(flow, true);
        Assert.True(TInterface.TFlowShortcutRun(flow, "SectionAdd"));
        Assert.Single(TInterface.TFlowSectionsRead(flow));
        Assert.False(TInterface.TFlowShortcutRun(flow, "Nothing"));

        TInterface.TFlowCommandSet(flow, false);
        Assert.False(TInterface.TFlowShortcutRun(flow, "ZoomIn"));
    }

    [Fact]
    public void Wheel_SeeksByDefault_AndReportsHandled()
    {
        LFlow flow = TFlowBuild();
        List<TimeSpan> seeks = [];
        TInterface.TFlowCursorAttach(flow, () => { }, seeks.Add);
        Assert.False(TInterface.TFlowWheelHandle(flow, 0));
        Assert.True(TInterface.TFlowWheelHandle(flow, 120));
        Assert.Single(seeks);
        Assert.True(seeks[0] > TimeSpan.Zero);
    }

    [Fact]
    public void Name_EditsTheSelection_AndCommitsTrimmedText()
    {
        LFlow flow = TFlowBuild();
        TInterface.TFlowSectionSet(flow, true);
        List<string> shows = [];
        int closes = 0;
        TInterface.TFlowNameAttach(flow, prompt => shows.Add(prompt.LFlowNameText), () => closes++);

        Assert.False(TInterface.TFlowNameStart(flow));
        TInterface.TFlowSectionAdd(flow);
        Assert.True(TInterface.TFlowNameStart(flow));
        Assert.Equal(0, flow.LFlowName.LFlowNameIndex);
        Assert.Equal([""], shows);

        TInterface.TFlowNameCommit(flow, " named ", "", "");
        Assert.Null(flow.LFlowName.LFlowNameIndex);
        Assert.Equal(1, closes);
        Assert.Equal("named", TInterface.TFlowSectionsRead(flow)[0].LPieceName);

        TInterface.TFlowNameHide(flow);
        Assert.Equal(1, closes);
        Assert.Equal(20, TInterface.TFlowOffsetResolve(false, 10, 40, 20));
        Assert.Equal(0, TInterface.TFlowOffsetResolve(true, 10, 40, 20));
    }

    [Fact]
    public void Attach_And_Clear_RaiseTheStripNotices()
    {
        LFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowUnloadSet(flow);
        List<string> notices = [];
        TInterface.TFlowStripAttach(flow, () => notices.Add("attach"), () => notices.Add("clear"));
        TInterface.TFlowMediaAttach(flow, () => notices.Add("media"));

        Assert.False(TInterface.TFlowClear(flow));
        TInterface.TFlowAttach(
            flow,
            TInterface.TViewerInfoCreate(TimeSpan.FromSeconds(60), 640, 360),
            "C:\\media\\clip.mp4",
            TimeSpan.FromSeconds(4));
        Assert.Equal(TimeSpan.FromSeconds(4), flow.LFlowCursor);
        Assert.Equal("0:04", TInterface.TCursorTimeFormat(flow.LFlowCursor));
        Assert.Equal("1:00", flow.LFlowLabelDuration);
        Assert.True(TInterface.TFlowClear(flow));
        Assert.Equal(["attach", "media", "clear", "media"], notices);

        TInterface.TFlowClose(flow);
        Assert.True(flow.LFlowUnloaded);
    }
}
