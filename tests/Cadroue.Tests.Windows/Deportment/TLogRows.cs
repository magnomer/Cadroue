using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TLogRows
{
    [Fact]
    public void Row_CarriesTheStringsTheWindowShows()
    {
        QLogRow row = TInterface.TLogRowCreate(
            TInterface.TTraceEntryCreate(LTraceKind.LTraceError, "boom", "stack", 1500));

        Assert.Equal("Log.Category.Error", row.QLogRowKey);
        Assert.Equal("12:00:00.000", row.QLogRowTime);
        Assert.Equal("boom", row.QLogRowSummary);
        Assert.Equal("stack", row.QLogRowDetail);
        Assert.True(row.QLogRowDetailed);
        Assert.NotEqual(string.Empty, row.QLogRowSpan);
        Assert.EndsWith("▾", row.QLogRowChip);
    }

    [Fact]
    public void Row_WithoutDetailOrSpan_IsPlain()
    {
        QLogRow row = TInterface.TLogRowCreate(TInterface.TTraceEntryCreate(LTraceKind.LTraceInfo, "hi", null, null));

        Assert.Equal("Log.Category.Info", row.QLogRowKey);
        Assert.Equal(string.Empty, row.QLogRowDetail);
        Assert.False(row.QLogRowDetailed);
        Assert.Equal(string.Empty, row.QLogRowSpan);
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
        QLog log = TInterface.TLogCreate();
        int resets = 0;
        TInterface.TLogResetAttach(log, () => resets++);

        TInterface.TLogCategorySet(log, ["Error"]);
        TInterface.TLogCategorySet(log, []);

        Assert.Equal(2, resets);
        Assert.Empty(log.QLogFeed.QLogRowsShown);
    }

    [Fact]
    public void Scroll_FollowsOnlyWhenTheFeedRestsAtTheTail()
    {
        QLog log = TInterface.TLogCreate();

        TInterface.TLogScrollHandle(log, true, 0, 1000, 500, 26);
        Assert.False(log.QLogFollowTail);

        TInterface.TLogScrollHandle(log, true, 40, 1000, 990, 26);
        Assert.False(log.QLogFollowTail);

        TInterface.TLogScrollHandle(log, false, 0, 1000, 990, 26);
        Assert.False(log.QLogFollowTail);

        TInterface.TLogScrollHandle(log, true, 0, 1000, 990, 26);
        Assert.True(log.QLogFollowTail);

        TInterface.TLogScrollHandle(log, true, 0, null, null, 26);
        Assert.True(log.QLogFollowTail);
    }
}
