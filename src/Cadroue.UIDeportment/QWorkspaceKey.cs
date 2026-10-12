namespace Cadroue.UIDeportment;

public sealed class QWorkspaceKey
{
    private const string LWorkspaceSplitKey = "Split";
    private const string LWorkspaceFixKey = "Fix";
    private const string LWorkspaceEditKey = "Edit";
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

    public bool LWorkspaceWorklist =>
        string.Equals(lWorkspaceKey, LWorkspaceWorklistKey, StringComparison.Ordinal);

    public bool LWorkspaceListPresent => !LWorkspaceWorklist;

    public bool LWorkspaceFlowPresent => !LWorkspaceWorklist;

    public bool LWorkspaceEdit => string.Equals(lWorkspaceKey, LWorkspaceEditKey, StringComparison.Ordinal);

    public string LWorkspaceNotice => LWorkspaceEdit ? "ExportPreset.DisabledTooltip" : "ExportPreset.AudioTooltip";

    public bool LWorkspaceAudioOnly => string.Equals(lWorkspaceKey, LWorkspaceAudioKey, StringComparison.Ordinal);

    public bool LWorkspaceSectionVisible => string.Equals(lWorkspaceKey, LWorkspaceSplitKey, StringComparison.Ordinal);

    public bool LWorkspaceSmart => lWorkspaceKey is LWorkspaceSplitKey or LWorkspaceFixKey;
}
