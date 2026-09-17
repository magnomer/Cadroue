namespace Cadroue.UIDeportment;

public sealed class LLogRow
{
    public bool LLogRowExpanded { get; internal set; }
}

public sealed class LLog
{
    private string lLogFilePath = string.Empty;
    private bool lLogFileLive = true;
    private bool lLogFollowTail = true;
    private long lLogSnapshot;

    public string LLogFilePath => lLogFilePath;

    public bool LLogFileLive => lLogFileLive;

    public bool LLogFollowTail => lLogFollowTail;

    public long LLogSnapshot => lLogSnapshot;

    public bool LLogFileCheck(string lPath) =>
        string.Equals(lLogFilePath, lPath, StringComparison.OrdinalIgnoreCase);

    public void LLogFileSet(string lPath, string lLivePath)
    {
        lLogFilePath = lPath;
        LLogLiveSet(lLivePath);
    }

    public bool LLogLiveSet(string lLivePath)
    {
        lLogFileLive = LLogFileCheck(lLivePath);
        return lLogFileLive;
    }

    public void LLogFollowSet(bool lFollowTail) => lLogFollowTail = lFollowTail;

    public void LLogSnapshotSet(long lSequence) => lLogSnapshot = lSequence;

    public bool LLogSnapshotCheck(long lSequence) => lSequence <= lLogSnapshot;

    public bool LLogExpandToggle(LLogRow lRow)
    {
        lRow.LLogRowExpanded = !lRow.LLogRowExpanded;
        return lRow.LLogRowExpanded;
    }
}
