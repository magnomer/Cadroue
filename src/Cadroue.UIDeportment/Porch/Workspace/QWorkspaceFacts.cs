namespace Cadroue.UIDeportment;

public sealed class QWorkspaceFacts
{
    private const string QWorkspaceSplitKey = "Split";
    private const string QWorkspaceFixKey = "Fix";
    private const string QWorkspaceEditKey = "Edit";
    private const string QWorkspaceAudioKey = "Audio";
    private const string QWorkspaceMergeKey = "Merge";
    private const string QWorkspaceWorklistKey = "Worklist";
    private readonly string qWorkspaceKey;

    public QWorkspaceFacts(string lKey)
    {
        qWorkspaceKey = lKey;
    }

    public bool QWorkspaceSourcePresent =>
        qWorkspaceKey is not (QWorkspaceMergeKey or QWorkspaceWorklistKey);

    public bool QWorkspaceWorklist =>
        string.Equals(qWorkspaceKey, QWorkspaceWorklistKey, StringComparison.Ordinal);

    public bool QWorkspaceListPresent => !QWorkspaceWorklist;

    public bool QWorkspaceFlowPresent => !QWorkspaceWorklist;

    public bool QWorkspaceEdit => string.Equals(qWorkspaceKey, QWorkspaceEditKey, StringComparison.Ordinal);

    public string QWorkspaceNotice => QWorkspaceEdit ? "ExportPreset.DisabledTooltip" : "ExportPreset.AudioTooltip";

    public bool QWorkspaceAudioOnly => string.Equals(qWorkspaceKey, QWorkspaceAudioKey, StringComparison.Ordinal);

    public bool QWorkspaceSectionVisible => string.Equals(qWorkspaceKey, QWorkspaceSplitKey, StringComparison.Ordinal);

    public bool QWorkspaceSmart => qWorkspaceKey is QWorkspaceSplitKey or QWorkspaceFixKey;
}
