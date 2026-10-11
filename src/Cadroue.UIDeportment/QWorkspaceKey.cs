namespace Cadroue.UIDeportment;

public sealed class QWorkspaceKey
{
    private const string LWorkspaceSplitKey = "Split";
    private const string LWorkspaceAudioKey = "Audio";
    private const string LWorkspaceMergeKey = "Merge";
    private const string LWorkspaceWorklistKey = "Worklist";
    private readonly string lWorkspaceKey;

    public QWorkspaceKey(string lKey)
    {
        lWorkspaceKey = lKey;
    }

    public bool LWorkspaceSourcePresent =>
        lWorkspaceKey is not (LWorkspaceMergeKey or LWorkspaceWorklistKey);

    public bool LWorkspaceAudioOnly => string.Equals(lWorkspaceKey, LWorkspaceAudioKey, StringComparison.Ordinal);

    public bool LWorkspaceSectionVisible => string.Equals(lWorkspaceKey, LWorkspaceSplitKey, StringComparison.Ordinal);
}
