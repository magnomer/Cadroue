using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TLogFollow
{
    [Fact]
    public void FileSet_TracksLiveByPathIgnoringCase()
    {
        QLog log = TInterface.TLogCreate();

        TInterface.TLogFileSet(log, "C:/logs/today.log", "c:/LOGS/today.log");
        Assert.True(log.QLogFileLive);
        Assert.True(TInterface.TLogFileCheck(log, "c:/logs/TODAY.log"));

        Assert.False(TInterface.TLogLiveSet(log, "C:/logs/tomorrow.log"));
        Assert.False(log.QLogFileLive);
    }

    [Fact]
    public void Snapshot_GatesOlderSequences()
    {
        QLog log = TInterface.TLogCreate();
        TInterface.TLogSnapshotSet(log, 10);

        Assert.True(TInterface.TLogSnapshotCheck(log, 10));
        Assert.True(TInterface.TLogSnapshotCheck(log, 3));
        Assert.False(TInterface.TLogSnapshotCheck(log, 11));
    }

    [Fact]
    public void Follow_DefaultsOn_AndToggles()
    {
        QLog log = TInterface.TLogCreate();
        Assert.True(log.QLogFollowTail);

        TInterface.TLogFollowSet(log, false);
        Assert.False(log.QLogFollowTail);
    }

    [Fact]
    public void Expand_TogglesRow()
    {
        QLog log = TInterface.TLogCreate();
        QLogRow row = TInterface.TLogRowCreate(
            TInterface.TTraceEntryCreate(LTraceKind.LTraceInfo, "hello", null, null));

        Assert.True(TInterface.TLogExpandToggle(log, row));
        Assert.True(row.QLogRowExpanded);
        Assert.True(TInterface.TLogExpandToggle(log, row));
        Assert.False(row.QLogRowExpanded);
        Assert.False(TInterface.TLogExpandToggle(log, null));
    }
}
