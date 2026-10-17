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

        QConvertDescribe(lPriority, QConvertSelectedCreate(), lRelayTarget, lSourceTab);
    }

    public void QConvertAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QConvertPresetCheck())
        {
            return;
        }

        QConvertDescribe(LWorkPriority.LWorkPriorityNormal, QConvertUnlockedCreate(), lRelayTarget, lSourceTab);
    }

    public void QConvertItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QConvertPresetCheck())
        {
            return;
        }

        QConvertDescribe(LWorkPriority.LWorkPriorityNormal, QConvertItemsCreate(lPaths), lRelayTarget, lSourceTab);
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

    private void QConvertDescribe(
        LWorkPriority lPriority, IReadOnlyList<LWorkSource> lSources, Guid lRelayTarget, Guid lSourceTab) =>
        _ = LMessenger.LMessengerConvertDescribe(
            lPriority,
            lSources,
            qConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);

    private LWorkSource[] QConvertSelectedCreate() =>
        QConvertSelectedRead() is { } lSelected ? [QConvertSourceCreate(lSelected)] : [];

    private LWorkSource[] QConvertUnlockedCreate() =>
        qConvertDocket.LDocketUnlockedRead().Select(QConvertSourceCreate).ToArray();

    private LWorkSource[] QConvertItemsCreate(IReadOnlyList<string> lPaths) =>
        qConvertDocket.LDocketUnlockedRead()
            .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
            .Select(QConvertSourceCreate)
            .ToArray();

    private static LWorkSource QConvertSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
