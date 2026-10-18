using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

// The tab's media notices: a loaded or cleared viewer source attaches or clears the flow, looks for its Losslesscut
// project and settles a pending relay; a list that drops or clears the open media closes it or cancels its load.
public sealed class QWorkspaceMedia
{
    private readonly QWorkspace qWorkspace;
    private readonly CWorkspace qWorkspaceGate;

    public QWorkspaceMedia(QWorkspace lOwner, CWorkspace lWorkspace)
    {
        qWorkspace = lOwner;
        qWorkspaceGate = lWorkspace;
    }

    public event Action<LMediaInfo, string>? QWorkspaceFlowAttached;
    public event Action? QWorkspaceFlowCleared;
    public event Action? QWorkspaceMediaClosed;
    public event Action? QWorkspaceLosslesscutFound;

    public void QWorkspacePathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath))
        {
            QWorkspaceMediaClosed?.Invoke();
        }
    }

    public void QWorkspaceRemovedHandle(IReadOnlyList<string> lRemovedPaths)
    {
        (bool lCancelled, bool lClosed) = qWorkspaceGate.CWorkspaceRemovalRead(lRemovedPaths);
        if (lCancelled)
        {
            QWorkspaceLoadCancel();
        }

        if (lClosed)
        {
            QWorkspaceMediaClosed?.Invoke();
        }
    }

    public bool QWorkspaceMediaClear(IReadOnlySet<Guid> lCohorts)
    {
        string[] lKept = QWorkspaceKeptRead(lCohorts);
        (bool lClosed, bool lCancelled, bool lCleared) = qWorkspaceGate.CWorkspaceClearRead(lKept);
        if (lClosed)
        {
            QWorkspaceMediaClosed?.Invoke();
        }

        if (lCancelled)
        {
            QWorkspaceLoadCancel();
        }

        int lStaleRemoved = qWorkspace.QWorkspaceDocket?.LDocketStaleRemove(lCohorts) ?? 0;
        return lCleared || lStaleRemoved > 0;
    }

    private string[] QWorkspaceKeptRead(IReadOnlySet<Guid> lCohorts) =>
        qWorkspace.QWorkspaceDocket?.LDocketProtectedRead(lCohorts)
            .Select(lEntry => lEntry.LDocketEntryPath)
            .ToArray() ?? [];

    internal void QWorkspaceMediaHandle(LCargo lCargo)
    {
        QWorkspaceLosslesscutHandle(lCargo);
        QWorkspaceFlowHandle(lCargo);
        qWorkspace.QWorkspaceRelay.QWorkspaceRelayHandle(lCargo);
    }

    private void QWorkspaceFlowHandle(LCargo lCargo)
    {
        if (qWorkspace.QWorkspaceFlow is null)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is { } lMediaInfo)
        {
            QWorkspaceFlowAttached?.Invoke(lMediaInfo, lCargo.LCargoSourcePath);
            return;
        }

        QWorkspaceFlowCleared?.Invoke();
    }

    private void QWorkspaceLosslesscutHandle(LCargo lCargo)
    {
        if (qWorkspace.QWorkspaceFlow is not { } lFlow || !qWorkspace.QWorkspaceFacts.QWorkspaceSectionVisible)
        {
            return;
        }

        if (lCargo.LCargoMediaInfo is null || string.IsNullOrWhiteSpace(lCargo.LCargoSourcePath))
        {
            lFlow.QFlowStamp.QFlowLosslesscutSet(string.Empty);
            return;
        }

        if (lFlow.QFlowStamp.QFlowLosslesscutSet(LLosslesscut.LLosslesscutPathNormalize(lCargo.LCargoSourcePath)))
        {
            QWorkspaceLosslesscutFound?.Invoke();
        }
    }

    private void QWorkspaceLoadCancel() => qWorkspace.QWorkspaceViewer?.QViewerMedia.QViewerLoadCancel();
}
