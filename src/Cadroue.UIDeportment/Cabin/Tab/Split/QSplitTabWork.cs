using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

// The split work the tab sends: the selected, every unlocked or the named sources, under the chosen preset.
public sealed class QSplitTabWork
{
    private readonly LPresetSelection qSplitPreset;
    private readonly QList qSplitList;
    private readonly LDocket qSplitDocket;
    private readonly QFlow qSplitFlow;

    public QSplitTabWork(LPresetSelection lPreset, QList lList, QFlow lFlow)
    {
        qSplitPreset = lPreset;
        qSplitList = lList;
        qSplitDocket = lList.QListDocket;
        qSplitFlow = lFlow;
    }

    public event Action? QSplitPresetMissing;

    // The list's current entry while it stays unlocked: the one source a run or a sweep works on.
    public static LDocketEntry? QSplitSelectedRead(QList lList) =>
        lList.QListPathCurrent is { } lPath
        && lList.QListDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    public void QSplitRun(bool lUrgent, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QSplitPresetCheck() || QSplitSelectedRead(qSplitList) is not { } lSelected)
        {
            return;
        }

        LMessengerSplit.LMessengerSplitDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lSelected.LDocketEntryPath,
            qSplitFlow.QFlowSection.QFlowSplitRead(),
            qSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void QSplitAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QSplitPresetCheck())
        {
            return;
        }

        _ = LMessengerSplit.LMessengerSplitDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qSplitDocket.LDocketUnlockedRead().Select(QSplitSourceCreate).ToArray(),
            qSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QSplitItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QSplitPresetCheck())
        {
            return;
        }

        _ = LMessengerSplit.LMessengerSplitDescribe(
            LWorkPriority.LWorkPriorityNormal,
            QSplitSourcesResolve(lPaths),
            qSplitPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    private LWorkSource[] QSplitSourcesResolve(IReadOnlyList<string> lPaths) =>
        qSplitDocket.LDocketUnlockedRead()
            .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
            .Select(QSplitSourceCreate)
            .ToArray();

    private bool QSplitPresetCheck()
    {
        if (qSplitPreset.LPresetSelectionValid)
        {
            return true;
        }

        QSplitPresetMissing?.Invoke();
        return false;
    }

    private static LWorkSource QSplitSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
