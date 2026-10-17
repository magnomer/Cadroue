using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// Each run gesture is one gate call; the gate answers false when the preset is missing, which raises the notice.
public sealed class QConvertTab
{
    private readonly CConvert qConvertGate;
    private readonly LPresetSelection qConvertPreset;
    private readonly QList qConvertList;
    private readonly LDocket qConvertDocket;

    public QConvertTab(LPresetSelection lPreset, QViewer lViewer, QList lList, CConvert lConvert)
    {
        qConvertGate = lConvert;
        qConvertPreset = lPreset;
        qConvertList = lList;
        qConvertDocket = lList.QListDocket;
        lList.QListPathChanged += lViewer.QViewerWorkspace.CWorkspaceOpen;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public event Action? QConvertPresetMissing;

    public void QConvertRun(bool lUrgent, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!qConvertGate.CConvertRun(
                qConvertPreset.LPresetSelectionValid,
                qConvertPreset.LPresetSelectionName,
                qConvertList.QListPathCurrent,
                qConvertDocket.LDocketItemsRead()
                    .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                    .ToArray(),
                lUrgent,
                lRelayTarget,
                lSourceTab))
        {
            QConvertPresetMissing?.Invoke();
        }
    }

    public void QConvertAllRun(Guid lRelayTarget, Guid lSourceTab) =>
        QConvertItemsRun(null, lRelayTarget, lSourceTab);

    // No paths runs every unlocked item; the gate picks the items and dispatches.
    public void QConvertItemsRun(IReadOnlyList<string>? lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!qConvertGate.CConvertAllRun(
                qConvertPreset.LPresetSelectionValid,
                qConvertPreset.LPresetSelectionName,
                qConvertDocket.LDocketItemsRead()
                    .Select(lItem => (lItem.LDocketEntryPath, lItem.LDocketEntryBatch, lItem.LDocketEntryLocked))
                    .ToArray(),
                lPaths,
                lRelayTarget,
                lSourceTab))
        {
            QConvertPresetMissing?.Invoke();
        }
    }
}
