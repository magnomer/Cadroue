using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QConvertTab
{
    private readonly LPresetSelection qConvertPreset;
    private readonly QList qConvertList;
    private readonly LDocket qConvertDocket;

    public QConvertTab(LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        qConvertPreset = lPreset;
        qConvertList = lList;
        qConvertDocket = lList.QListDocket;
        lList.QListPathChanged += lViewer.QViewerWorkspace.CWorkspaceOpen;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public event Action? QConvertPresetMissing;

    public void QConvertRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QConvertPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerConvertDescribe(
            lPriority,
            QConvertSelectedRead() is { } lSelected ? [QConvertSourceCreate(lSelected)] : [],
            qConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QConvertAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QConvertPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerConvertDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qConvertDocket.LDocketUnlockedRead().Select(QConvertSourceCreate).ToArray(),
            qConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QConvertItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QConvertPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerConvertDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qConvertDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(QConvertSourceCreate)
                .ToArray(),
            qConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    private bool QConvertPresetCheck()
    {
        if (qConvertPreset.LPresetSelectionValid)
        {
            return true;
        }

        QConvertPresetMissing?.Invoke();
        return false;
    }

    private LDocketEntry? QConvertSelectedRead() =>
        qConvertList.QListPathCurrent is { } lPath
        && qConvertDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource QConvertSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
