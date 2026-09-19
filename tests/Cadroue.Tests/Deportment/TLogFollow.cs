using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TLogFollow
{
    [Fact]
    public void FileSet_TracksLiveByPathIgnoringCase()
    {
        LLog log = TInterface.TLogCreate();

        TInterface.TLogFileSet(log, "C:/logs/today.log", "c:/LOGS/today.log");
        Assert.True(log.LLogFileLive);
        Assert.True(TInterface.TLogFileCheck(log, "c:/logs/TODAY.log"));

        Assert.False(TInterface.TLogLiveSet(log, "C:/logs/tomorrow.log"));
        Assert.False(log.LLogFileLive);
    }

    [Fact]
    public void Snapshot_GatesOlderSequences()
    {
        LLog log = TInterface.TLogCreate();
        TInterface.TLogSnapshotSet(log, 10);

        Assert.True(TInterface.TLogSnapshotCheck(log, 10));
        Assert.True(TInterface.TLogSnapshotCheck(log, 3));
        Assert.False(TInterface.TLogSnapshotCheck(log, 11));
    }

    [Fact]
    public void Follow_DefaultsOn_AndToggles()
    {
        LLog log = TInterface.TLogCreate();
        Assert.True(log.LLogFollowTail);

        TInterface.TLogFollowSet(log, false);
        Assert.False(log.LLogFollowTail);
    }

    [Fact]
    public void Expand_TogglesRow()
    {
        LLog log = TInterface.TLogCreate();
        LLogRow row = TInterface.TLogRowCreate(
            TInterface.TTraceEntryCreate(LTraceKind.LTraceInfo, "hello", null, null));

        Assert.True(TInterface.TLogExpandToggle(log, row));
        Assert.True(row.LLogRowExpanded);
        Assert.True(TInterface.TLogExpandToggle(log, row));
        Assert.False(row.LLogRowExpanded);
        Assert.False(TInterface.TLogExpandToggle(log, null));
    }
}
