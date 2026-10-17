using System;
using Cadroue.Conduct;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests;

public sealed class TEditGate
{
    private static readonly Guid TEditRelay = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TEditTab = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TEditBatchA = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TEditBatchB = new("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid TEditBatchC = new("cccccccc-0000-0000-0000-000000000003");

    private static readonly (string CEditPath, Guid CEditBatch, bool CEditLocked)[] TEditItems =
    [
        (@"C:\a.mp4", TEditBatchA, false),
        (@"C:\b.mp4", TEditBatchB, true),
        (@"C:\c.mp4", TEditBatchC, false),
    ];

    private readonly TEditPort tEditPort = new();
    private readonly CEdit tEditGate;

    public TEditGate()
    {
        tEditGate = TAtelier.TAtelierCreate(port => port with { LAtelierEdit = tEditPort })
            .CAtelierEdit;
    }

    [Fact]
    public void EditRun_InvalidPreset_AnswersMissingBeforeIncompatible()
    {
        Assert.Equal(
            (CEditVerdict.CEditVerdictMissing, (string?)null, Guid.Empty),
            tEditGate.CEditRun(false, false, @"C:\a.mp4", TEditItems));
        Assert.Equal(
            CEditVerdict.CEditVerdictMissing,
            tEditGate.CEditRun(false, true, null, TEditItems).CEditVerdict);
    }

    [Fact]
    public void EditRun_UnsupportedPreset_AnswersIncompatible() =>
        Assert.Equal(
            CEditVerdict.CEditVerdictIncompatible,
            tEditGate.CEditRun(true, false, @"C:\a.mp4", TEditItems).CEditVerdict);

    [Fact]
    public void EditRun_NoMissingOrLockedSelection_AnswersEmpty()
    {
        Assert.Equal(
            (CEditVerdict.CEditVerdictEmpty, (string?)null, Guid.Empty),
            tEditGate.CEditRun(true, true, null, TEditItems));
        Assert.Equal(
            (CEditVerdict.CEditVerdictEmpty, (string?)null, Guid.Empty),
            tEditGate.CEditRun(true, true, @"C:\z.mp4", TEditItems));
        Assert.Equal(
            (CEditVerdict.CEditVerdictEmpty, (string?)null, Guid.Empty),
            tEditGate.CEditRun(true, true, @"C:\b.mp4", TEditItems));
    }

    [Fact]
    public void EditRun_UnlockedSelection_AnswersItsPathAndBatchIgnoringCaseWithoutThePort()
    {
        Assert.Equal(
            (CEditVerdict.CEditVerdictRan, @"C:\c.mp4", TEditBatchC),
            tEditGate.CEditRun(true, true, @"c:\C.MP4", TEditItems));
        Assert.Empty(tEditPort.TEditCalls);
    }

    [Fact]
    public void EditAllRun_PresetRefused_AsksNoPort()
    {
        Assert.Equal(
            CEditVerdict.CEditVerdictMissing,
            tEditGate.CEditAllRun(false, false, "Alpha", TEditItems, null, TEditRelay, TEditTab));
        Assert.Equal(
            CEditVerdict.CEditVerdictIncompatible,
            tEditGate.CEditAllRun(true, false, "Alpha", TEditItems, null, TEditRelay, TEditTab));
        Assert.Empty(tEditPort.TEditCalls);
    }

    [Fact]
    public void EditAllRun_NoPaths_RunsEveryUnlockedItemInOrderOnce()
    {
        Assert.Equal(
            CEditVerdict.CEditVerdictRan,
            tEditGate.CEditAllRun(true, true, "Alpha", TEditItems, null, TEditRelay, TEditTab));

        Assert.Equal(
            [$@"all:Alpha|C:\a.mp4@{TEditBatchA:N},C:\c.mp4@{TEditBatchC:N}|{TEditRelay}|{TEditTab}"],
            tEditPort.TEditCalls);
    }

    [Fact]
    public void EditAllRun_Paths_RunsUnlockedItemsAmongThemIgnoringCase()
    {
        tEditGate.CEditAllRun(
            true, true, "Alpha", TEditItems, [@"c:\C.MP4", @"C:\b.mp4"], TEditRelay, TEditTab);

        Assert.Equal([$@"all:Alpha|C:\c.mp4@{TEditBatchC:N}|{TEditRelay}|{TEditTab}"], tEditPort.TEditCalls);
    }

    [Fact]
    public void EditAllRun_NothingPicked_StillAsksThePortOnce()
    {
        tEditGate.CEditAllRun(true, true, "Alpha", TEditItems, [@"C:\b.mp4"], TEditRelay, TEditTab);

        Assert.Equal([$"all:Alpha||{TEditRelay}|{TEditTab}"], tEditPort.TEditCalls);
    }
}
