using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QAudioTab
{
    private readonly LPresetSelection qAudioPreset;
    private readonly QInspectorAudio qAudioSections;
    private readonly QViewer qAudioViewer;
    private readonly QList qAudioList;
    private readonly LDocket qAudioDocket;

    public QAudioTab(
        CUsher lUsher,
        CInspector lInspector,
        CEditAudio lEditAudio,
        LPresetSelection lPreset,
        QViewer lViewer,
        QList lList)
    {
        QAudioInspector = new QInspector(lInspector);
        QAudioStore = new QAudioTabPlan(this, lUsher, lEditAudio, lList);
        qAudioPreset = lPreset;
        qAudioSections = QAudioInspector.QInspectorAudio;
        qAudioViewer = lViewer;
        qAudioList = lList;
        qAudioDocket = lList.QListDocket;
        QAudioInspectorAttach();
        QAudioProcessing.QProcessingOrderedSet(true);
        QAudioViewerAttach(lViewer, lList);
    }

    public event Action? QAudioPresetMissing;
    public event Action? QAudioPresetIncompatible;
    public event Action? QAudioViewerDeferred;

    public QInspector QAudioInspector { get; }

    public QProcessing QAudioProcessing { get; } = new();

    public QSMonitor QAudioMonitor { get; } = new();

    public QAudioTabPlan QAudioStore { get; }

    // The engine reads which audio tool a step names.
    public static LAudioKind? QAudioKindRead(string lStep) => LAudioPort.LAudioKindRead(lStep);

    public void QAudioClose()
    {
        QAudioDetach();
        QAudioMonitor.Dispose();
    }

    private void QAudioInspectorAttach()
    {
        QAudioInspector.QInspectorPersistentChanged += QAudioStore.QAudioPersistentSave;
        qAudioSections.QInspectorAudioChanged += QAudioChangeHandle;
        QAudioInspector.QInspectorGate.CInspectorSkip.CSkipActiveChanged += QAudioSkipHandle;
    }

    private void QAudioViewerAttach(QViewer lViewer, QList lList)
    {
        QAudioProcessing.QProcessingOrderChanged += QAudioStore.QAudioStateSave;
        lViewer.QViewerMediaChanged += QAudioMediaHandle;
        lList.QListPathChanged += lViewer.QViewerWorkspace.CWorkspaceOpen;
        lViewer.QViewerOpenNotice += QAudioOpenHandle;
        lList.QListItemsAdded += QAudioStore.QAudioItemsHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    private void QAudioDetach()
    {
        QAudioInspector.QInspectorPersistentChanged -= QAudioStore.QAudioPersistentSave;
        qAudioSections.QInspectorAudioChanged -= QAudioChangeHandle;
        QAudioInspector.QInspectorGate.CInspectorSkip.CSkipActiveChanged -= QAudioSkipHandle;
        QAudioProcessing.QProcessingOrderChanged -= QAudioStore.QAudioStateSave;
        qAudioViewer.QViewerMediaChanged -= QAudioMediaHandle;
        qAudioViewer.QViewerOpenNotice -= QAudioOpenHandle;
    }

    public LSceneTabEntry QAudioLayoutRead(LSceneTabEntry lLayout)
    {
        if (qAudioSections.QInspectorPersistentCheck())
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorAudio = LAudioSidecar.LAudioPersistentCreate(qAudioSections.QInspectorPersistentRead()),
                LSceneInspectorSkip = QAudioInspector.QInspectorSkip.QSkipPersistent
            };
        }

        return lLayout;
    }

    public void QAudioLayoutApply(LSceneTabEntry? lLayout)
    {
        if (lLayout?.LSceneInspector is { LSceneInspectorAudio: { } lRecord } lInspector)
        {
            QAudioInspector.QInspectorSaveSuspend();
            try
            {
                LWorkAudio lPlan = LAudioSidecar.LAudioPersistentRead(lRecord);
                qAudioSections.QInspectorAudioApply(lPlan);
                qAudioSections.QInspectorPersistentApply(lInspector.LSceneInspectorSkip);
            }
            finally
            {
                QAudioInspector.QInspectorSaveResume();
            }
        }

        QAudioStore.QAudioActiveUpdate();
    }

    public void QAudioRun(bool lUrgent, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QAudioPresetCheck() || QAudioSelectedRead() is not { } lSelected)
        {
            return;
        }

        QAudioStore.QAudioStateSave();
        LMessengerAudio.LMessengerAudioDescribe(
            lUrgent ? LWorkPriority.LWorkPriorityHigh : LWorkPriority.LWorkPriorityNormal,
            lSelected.LDocketEntryPath,
            QAudioStore.QAudioPlanRead(),
            qAudioPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab,
            lSelected.LDocketEntryBatch);
    }

    public void QAudioAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QAudioPresetCheck())
        {
            return;
        }

        QAudioStore.QAudioStateSave();
        LMessengerAudio.LMessengerAudioDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qAudioDocket.LDocketUnlockedRead().Select(QAudioSourceCreate).ToArray(),
            qAudioPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void QAudioItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QAudioPresetCheck())
        {
            return;
        }

        QAudioStore.QAudioStateSave();
        LMessengerAudio.LMessengerAudioDescribe(
            LWorkPriority.LWorkPriorityNormal,
            QAudioSourcesRead(lPaths),
            qAudioPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    // Before the viewer opens a new path: the plan of the source it leaves is saved.
    private void QAudioOpenHandle(string lPath) => QAudioStore.QAudioStateSave();

    public void QAudioSkipHandle()
    {
        QAudioProcessing.QProcessingSkipSet(QAudioInspector.QInspectorSkip.QSkipActive);
        QAudioStore.QAudioStateSave();
    }

    public void QAudioChangeHandle()
    {
        QAudioStore.QAudioActiveUpdate();
        QAudioStore.QAudioStateSave();
        QAudioMonitor.QSMonitorWaveform.QSMonitorPlanApply(QAudioStore.QAudioPlanRead());
        QAudioViewerDeferred?.Invoke();
    }

    public void QAudioViewerApply() => qAudioViewer.QViewerAudio.QViewerGraphSet(QAudioGraphFormat());

    private string QAudioGraphFormat()
    {
        LWorkAudio lPlan = QAudioStore.QAudioPlanRead();
        return lPlan.LWorkAudioSkip ? string.Empty : LWorkAudioFilter.LWorkAudioFormat(lPlan, QAudioRateRead());
    }

    private void QAudioMediaHandle(LCargo lCargo)
    {
        bool lOwnerFirst = QAudioInspector.QInspectorOwnerSet(lCargo.LCargoSourcePath);
        QAudioStore.QAudioPlanRestore(lCargo.LCargoSourcePath, lOwnerFirst);
        QAudioMonitor.QSMonitorWaveform.QSMonitorSourceOpen(
            lCargo.LCargoSourcePath,
            QAudioDurationRead(lCargo),
            QAudioRateRead());
        QAudioMonitor.QSMonitorWaveform.QSMonitorPlanApply(QAudioStore.QAudioPlanRead());
    }

    private static TimeSpan QAudioDurationRead(LCargo lCargo) =>
        lCargo.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero;

    private int QAudioRateRead() => qAudioViewer.QViewerMediaInfo?.LMediaSampleRate ?? 0;

    private bool QAudioPresetCheck()
    {
        if (!qAudioPreset.LPresetSelectionValid)
        {
            QAudioPresetMissing?.Invoke();
            return false;
        }

        if (!QAudioEncodingCheck())
        {
            QAudioPresetIncompatible?.Invoke();
            return false;
        }

        return true;
    }

    // A preset with no encoding passes; an encoding passes only when it supports audio work.
    private bool QAudioEncodingCheck() =>
        qAudioPreset.LPresetSelectionEncoding is not { } lEncoding
        || lEncoding.LEncodingSupportCheck(LWorkKind.LWorkKindAudio);

    private LWorkSource[] QAudioSourcesRead(IReadOnlyList<string> lPaths) =>
        qAudioDocket.LDocketUnlockedRead()
            .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
            .Select(QAudioSourceCreate)
            .ToArray();

    private LDocketEntry? QAudioSelectedRead() =>
        qAudioList.QListPathCurrent is { } lPath
        && qAudioDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource QAudioSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
