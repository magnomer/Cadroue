using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QAudioTab
{
    public static readonly IReadOnlyList<QProcessingRow> QAudioRows =
    [
        new("High Pass", "/PAsset/PPanel/PProcessingHighPass.svg", "Processing.Step.HighPass"),
        new("Low Pass", "/PAsset/PPanel/PProcessingLowPass.svg", "Processing.Step.LowPass"),
        new("Noise Reduction", "/PAsset/PPanel/PProcessingNoiseReduction.svg", "Processing.Step.NoiseReduction"),
        new("Equalizer", "/PAsset/PPanel/PProcessingEqualizer.svg", "Processing.Step.Equalizer"),
        new("Volume", "/PAsset/PPanel/PProcessingVolume.svg", "Processing.Step.Volume"),
        new("Normalize", "/PAsset/PPanel/PProcessingNormalize.svg", "Processing.Step.Normalize"),
    ];

    private static readonly IReadOnlyDictionary<string, LAudioKind> qAudioKinds = new Dictionary<string, LAudioKind>
    {
        ["Volume"] = LAudioKind.LAudioKindVolume,
        ["Normalize"] = LAudioKind.LAudioKindLeveling,
        ["Noise Reduction"] = LAudioKind.LAudioKindDenoise,
        ["High Pass"] = LAudioKind.LAudioKindHighpass,
        ["Low Pass"] = LAudioKind.LAudioKindLowpass,
        ["Equalizer"] = LAudioKind.LAudioKindEqualizer,
    };

    private readonly LPresetSelection qAudioPreset;
    private readonly QInspectorAudio qAudioSections;
    private readonly QViewer qAudioViewer;
    private readonly QList qAudioList;
    private readonly LDocket qAudioDocket;

    public QAudioTab(CUsher lUsher, CInspector lInspector, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        QAudioInspector = new QInspector(lInspector);
        QAudioStore = new QAudioTabPlan(this, lUsher, lList);
        qAudioPreset = lPreset;
        qAudioSections = QAudioInspector.QInspectorAudio;
        QAudioInspector.QInspectorPersistentChanged += QAudioStore.QAudioPersistentSave;
        qAudioSections.QInspectorAudioChanged += QAudioChangeHandle;
        QAudioInspector.QInspectorSkip.QSkipActiveChanged += QAudioSkipHandle;
        qAudioViewer = lViewer;
        qAudioList = lList;
        qAudioDocket = lList.QListDocket;
        QAudioProcessing.QProcessingOrderedSet(true);
        QAudioProcessing.QProcessingOrderChanged += QAudioStore.QAudioStateSave;
        lViewer.QViewerMediaChanged += QAudioMediaHandle;
        lList.QListPathChanged += lViewer.QViewerWorkspace.CWorkspaceOpen;
        lViewer.QViewerOpenNotice += QAudioOpenHandle;
        lList.QListItemsAdded += QAudioStore.QAudioItemsHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public event Action? QAudioPresetMissing;
    public event Action? QAudioPresetIncompatible;
    public event Action? QAudioViewerDeferred;

    public QInspector QAudioInspector { get; }

    public QProcessing QAudioProcessing { get; } = new();

    public QSMonitor QAudioMonitor { get; } = new();

    public QAudioTabPlan QAudioStore { get; }

    public static LAudioKind? QAudioKindRead(string lStep) =>
        qAudioKinds.TryGetValue(lStep, out LAudioKind lKind) ? lKind : null;

    public void QAudioClose()
    {
        QAudioInspector.QInspectorPersistentChanged -= QAudioStore.QAudioPersistentSave;
        qAudioSections.QInspectorAudioChanged -= QAudioChangeHandle;
        QAudioInspector.QInspectorSkip.QSkipActiveChanged -= QAudioSkipHandle;
        QAudioProcessing.QProcessingOrderChanged -= QAudioStore.QAudioStateSave;
        qAudioViewer.QViewerMediaChanged -= QAudioMediaHandle;
        qAudioViewer.QViewerOpenNotice -= QAudioOpenHandle;
        QAudioMonitor.Dispose();
    }

    public LSceneTabEntry QAudioLayoutRead(LSceneTabEntry lLayout)
    {
        if (qAudioSections.QInspectorPersistentCheck())
        {
            lLayout.LSceneInspector = new LSceneInspectorEntry
            {
                LSceneInspectorAudio = LAudio.LAudioPersistentCreate(qAudioSections.QInspectorPersistentRead()),
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
                LWorkAudio lPlan = LAudio.LAudioPersistentRead(lRecord);
                qAudioSections.QInspectorAudioApply(lPlan);
                qAudioSections.QInspectorPersistentApply(lPlan, lInspector.LSceneInspectorSkip);
                QAudioInspector.QInspectorSkip.QSkipActiveSet(lPlan.LWorkAudioSkip);
            }
            finally
            {
                QAudioInspector.QInspectorSaveResume();
            }
        }

        QAudioStore.QAudioActiveUpdate();
    }

    public void QAudioRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QAudioPresetCheck() || QAudioSelectedRead() is not { } lSelected)
        {
            return;
        }

        QAudioStore.QAudioStateSave();
        LMessenger.LMessengerAudioDescribe(
            lPriority,
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
        LMessenger.LMessengerAudioDescribe(
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
        LMessenger.LMessengerAudioDescribe(
            LWorkPriority.LWorkPriorityNormal,
            qAudioDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(QAudioSourceCreate)
                .ToArray(),
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

    public void QAudioViewerApply()
    {
        LWorkAudio lPlan = QAudioStore.QAudioPlanRead();
        qAudioViewer.QViewerAudio.QViewerGraphSet(
            lPlan.LWorkAudioSkip ? string.Empty : lPlan.LWorkAudioFormat(QAudioRateRead()));
    }

    private void QAudioMediaHandle(LCargo lCargo)
    {
        bool lOwnerFirst = QAudioInspector.QInspectorOwnerSet(lCargo.LCargoSourcePath);
        QAudioStore.QAudioPlanRestore(lCargo.LCargoSourcePath, lOwnerFirst);
        QAudioMonitor.QSMonitorWaveform.QSMonitorSourceOpen(
            lCargo.LCargoSourcePath,
            lCargo.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero,
            QAudioRateRead());
        QAudioMonitor.QSMonitorWaveform.QSMonitorPlanApply(QAudioStore.QAudioPlanRead());
    }

    private int QAudioRateRead() => qAudioViewer.QViewerMediaInfo?.LMediaSampleRate ?? 0;

    private bool QAudioPresetCheck()
    {
        if (!qAudioPreset.LPresetSelectionValid)
        {
            QAudioPresetMissing?.Invoke();
            return false;
        }

        if (qAudioPreset.LPresetSelectionEncoding is { } lEncoding
            && !lEncoding.LEncodingSupportCheck(LWorkKind.LWorkKindAudio))
        {
            QAudioPresetIncompatible?.Invoke();
            return false;
        }

        return true;
    }

    private LDocketEntry? QAudioSelectedRead() =>
        qAudioList.QListPathCurrent is { } lPath
        && qAudioDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource QAudioSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}
