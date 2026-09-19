using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TLogRows
{
    [Fact]
    public void Row_CarriesTheStringsTheWindowShows()
    {
        LLogRow row = TInterface.TLogRowCreate(
            TInterface.TTraceEntryCreate(LTraceKind.LTraceError, "boom", "stack", 1500));

        Assert.Equal("Log.Category.Error", row.LLogRowKey);
        Assert.Equal("12:00:00.000", row.LLogRowTime);
        Assert.Equal("boom", row.LLogRowSummary);
        Assert.Equal("stack", row.LLogRowDetail);
        Assert.True(row.LLogRowDetailed);
        Assert.NotEqual(string.Empty, row.LLogRowSpan);
        Assert.EndsWith("▾", row.LLogRowChip);
    }

    [Fact]
    public void Row_WithoutDetailOrSpan_IsPlain()
    {
        LLogRow row = TInterface.TLogRowCreate(TInterface.TTraceEntryCreate(LTraceKind.LTraceInfo, "hi", null, null));

        Assert.Equal("Log.Category.Info", row.LLogRowKey);
        Assert.Equal(string.Empty, row.LLogRowDetail);
        Assert.False(row.LLogRowDetailed);
        Assert.Equal(string.Empty, row.LLogRowSpan);
    }

    [Fact]
    public void KeyRead_MapsEveryKind()
    {
        Assert.Equal("Log.Category.Work", TInterface.TLogKeyRead(LTraceKind.LTraceWork));
        Assert.Equal("Log.Category.Interface", TInterface.TLogKeyRead(LTraceKind.LTraceUi));
        Assert.Equal("Log.Category.Info", TInterface.TLogKeyRead(LTraceKind.LTraceInfo));
    }

    [Fact]
    public void CategorySet_ResetsTheShownRows()
    {
        LLog log = TInterface.TLogCreate();
        int resets = 0;
        TInterface.TLogResetAttach(log, () => resets++);

        TInterface.TLogCategorySet(log, ["Error"]);
        TInterface.TLogCategorySet(log, []);

        Assert.Equal(2, resets);
        Assert.Empty(log.LLogRowsShown);
    }

    [Fact]
    public void Scroll_FollowsOnlyWhenTheFeedRestsAtTheTail()
    {
        LLog log = TInterface.TLogCreate();

        TInterface.TLogScrollHandle(log, true, 0, 1000, 500, 26);
        Assert.False(log.LLogFollowTail);

        TInterface.TLogScrollHandle(log, true, 40, 1000, 990, 26);
        Assert.False(log.LLogFollowTail);

        TInterface.TLogScrollHandle(log, false, 0, 1000, 990, 26);
        Assert.False(log.LLogFollowTail);

        TInterface.TLogScrollHandle(log, true, 0, 1000, 990, 26);
        Assert.True(log.LLogFollowTail);

        TInterface.TLogScrollHandle(log, true, 0, null, null, 26);
        Assert.True(log.LLogFollowTail);
    }
}
