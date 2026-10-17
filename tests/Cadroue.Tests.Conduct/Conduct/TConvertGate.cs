using System;
using Cadroue.Conduct;
using Xunit;

namespace Cadroue.Tests;

public sealed class TConvertGate
{
    private static readonly Guid TConvertRelay = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TConvertTab = new("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TConvertBatchA = new("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid TConvertBatchB = new("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid TConvertBatchC = new("cccccccc-0000-0000-0000-000000000003");

    private static readonly (string CConvertPath, Guid CConvertBatch, bool CConvertLocked)[] TConvertItems =
    [
        (@"C:\a.mp4", TConvertBatchA, false),
        (@"C:\b.mp4", TConvertBatchB, true),
        (@"C:\c.mp4", TConvertBatchC, false),
    ];

    private readonly TConvertPort tConvertPort = new();
    private readonly CConvert tConvertGate;

    public TConvertGate()
    {
        tConvertGate = TAtelier.TAtelierCreate(port => port with { LAtelierConvert = tConvertPort })
            .CAtelierConvert;
    }

    [Fact]
    public void ConvertRun_InvalidPreset_AnswersFalseAndRunsNothing()
    {
        Assert.False(tConvertGate.CConvertRun(
            false, "Alpha", @"C:\a.mp4", TConvertItems, true, TConvertRelay, TConvertTab));
        Assert.False(tConvertGate.CConvertAllRun(false, "Alpha", TConvertItems, null, TConvertRelay, TConvertTab));
        Assert.Empty(tConvertPort.TConvertCalls);
    }

    [Fact]
    public void ConvertRun_CurrentPath_RunsThatItemIgnoringCaseAtTheGivenPriority()
    {
        Assert.True(tConvertGate.CConvertRun(
            true, "Alpha", @"c:\C.MP4", TConvertItems, true, TConvertRelay, TConvertTab));
        Assert.True(tConvertGate.CConvertRun(
            true, "Alpha", @"C:\a.mp4", TConvertItems, false, TConvertRelay, TConvertTab));

        Assert.Equal(
            [
                $@"Alpha|C:\c.mp4@{TConvertBatchC:N}|urgent|{TConvertRelay}|{TConvertTab}",
                $@"Alpha|C:\a.mp4@{TConvertBatchA:N}|normal|{TConvertRelay}|{TConvertTab}",
            ],
            tConvertPort.TConvertCalls);
    }

    [Fact]
    public void ConvertRun_LockedMissingOrNoPath_StillAsksThePortWithNothing()
    {
        Assert.True(tConvertGate.CConvertRun(
            true, "Alpha", @"C:\b.mp4", TConvertItems, true, TConvertRelay, TConvertTab));
        Assert.True(tConvertGate.CConvertRun(
            true, "Alpha", @"C:\z.mp4", TConvertItems, true, TConvertRelay, TConvertTab));
        Assert.True(tConvertGate.CConvertRun(true, "Alpha", null, TConvertItems, true, TConvertRelay, TConvertTab));

        Assert.Equal(3, tConvertPort.TConvertCalls.Count);
        Assert.All(tConvertPort.TConvertCalls, call => Assert.StartsWith("Alpha||urgent|", call));
    }

    [Fact]
    public void ConvertAllRun_Paths_RunsUnlockedItemsAmongThemInDocketOrder()
    {
        Assert.True(tConvertGate.CConvertAllRun(true, "Alpha", TConvertItems, null, TConvertRelay, TConvertTab));
        Assert.True(tConvertGate.CConvertAllRun(
            true, "Alpha", TConvertItems, [@"C:\C.MP4", @"C:\b.mp4"], TConvertRelay, TConvertTab));

        Assert.Equal(
            [
                $@"Alpha|C:\a.mp4@{TConvertBatchA:N},C:\c.mp4@{TConvertBatchC:N}|normal|{TConvertRelay}|{TConvertTab}",
                $@"Alpha|C:\c.mp4@{TConvertBatchC:N}|normal|{TConvertRelay}|{TConvertTab}",
            ],
            tConvertPort.TConvertCalls);
    }

    [Fact]
    public void ConvertRun_NoItems_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => tConvertGate.CConvertRun(true, "Alpha", null, null!, true, default, default));
        Assert.Throws<ArgumentNullException>(
            () => tConvertGate.CConvertAllRun(true, "Alpha", null!, null, default, default));
    }
}
