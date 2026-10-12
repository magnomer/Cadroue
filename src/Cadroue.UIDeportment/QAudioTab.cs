using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.Media;
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

    private readonly CUsher qAudioUsher;
    private readonly LPresetSelection qAudioPreset;
    private readonly QInspectorAudio qAudioSections;
    private readonly QViewer qAudioViewer;
    private readonly QList qAudioList;
    private readonly LDocket qAudioDocket;

    public QAudioTab(CUsher lUsher, LPresetSelection lPreset, QViewer lViewer, QList lList)
    {
        qAudioUsher = lUsher;
        qAudioPreset = lPreset;
        qAudioSections = QAudioInspector.QInspectorAudio;
        QAudioInspector.QInspectorPersistentChanged += QAudioPersistentSave;
        qAudioSections.QInspectorAudioChanged += QAudioChangeHandle;
        QAudioInspector.QInspectorSkip.QSkipActiveChanged += QAudioSkipHandle;
        qAudioViewer = lViewer;
        qAudioList = lList;
        qAudioDocket = lList.QListDocket;
        QAudioProcessing.QProcessingOrderedSet(true);
        QAudioProcessing.QProcessingOrderChanged += QAudioStateSave;
        lViewer.QViewerMediaChanged += QAudioMediaHandle;
        lList.QListPathChanged += QAudioPathHandle;
        lList.QListItemsAdded += QAudioItemsHandle;
        lViewer.QViewerSource.QViewerPathsDrop += lDropPaths => _ = lList.QListPathsAdd(lDropPaths);
    }

    public event Action? QAudioPresetMissing;
    public event Action? QAudioPresetIncompatible;
    public event Action? QAudioViewerDeferred;

    public QInspector QAudioInspector { get; } = new();

    public QProcessing QAudioProcessing { get; } = new();

    public QSMonitor QAudioMonitor { get; } = new();

    public static LAudioKind? QAudioKindRead(string lStep) =>
        qAudioKinds.TryGetValue(lStep, out LAudioKind lKind) ? lKind : null;

    public void QAudioClose()
    {
        QAudioInspector.QInspectorPersistentChanged -= QAudioPersistentSave;
        qAudioSections.QInspectorAudioChanged -= QAudioChangeHandle;
        QAudioInspector.QInspectorSkip.QSkipActiveChanged -= QAudioSkipHandle;
        QAudioProcessing.QProcessingOrderChanged -= QAudioStateSave;
        qAudioViewer.QViewerMediaChanged -= QAudioMediaHandle;
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

        QAudioActiveUpdate();
    }

    public void QAudioRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!QAudioPresetCheck() || QAudioSelectedRead() is not { } lSelected)
        {
            return;
        }

        QAudioStateSave();
        LMessenger.LMessengerAudioDescribe(
            lPriority,
            lSelected.LDocketEntryPath,
            QAudioPlanRead(),
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

        QAudioStateSave();
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

        QAudioStateSave();
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

    public void QAudioPathHandle(string? lPath)
    {
        if (string.IsNullOrWhiteSpace(lPath) || qAudioViewer.QViewerSourceMatch(lPath))
        {
            return;
        }

        QAudioStateSave();
        qAudioViewer.QViewerPathHandle(lPath);
    }

    public void QAudioSkipHandle()
    {
        QAudioProcessing.QProcessingSkipSet(QAudioInspector.QInspectorSkip.QSkipActive);
        QAudioStateSave();
    }

    public void QAudioChangeHandle()
    {
        QAudioActiveUpdate();
        QAudioStateSave();
        QAudioMonitor.QSMonitorPlanApply(QAudioPlanRead());
        QAudioViewerDeferred?.Invoke();
    }

    public void QAudioItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        QAudioFanoutSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    public void QAudioPersistentSave() =>
        QAudioFanoutSave(qAudioDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public void QAudioViewerApply()
    {
        LWorkAudio lPlan = QAudioPlanRead();
        qAudioViewer.QViewerGraphSet(lPlan.LWorkAudioSkip ? string.Empty : lPlan.LWorkAudioFormat(QAudioRateRead()));
    }

    public LWorkAudio QAudioPlanRead() =>
        qAudioSections.QInspectorAudioRead(
            QAudioProcessing.QProcessingSteps.Select(QAudioKindRead).OfType<LAudioKind>());

    public void QAudioStateSave()
    {
        if (QAudioInspector.QInspectorSaveSuspended
            || QAudioInspector.QInspectorOwnerPath is not { } lSourcePath
            || qAudioDocket.LDocketLockCheck(lSourcePath))
        {
            return;
        }

        LWorkAudio lPlan = QAudioPlanRead();
        if (!lPlan.LWorkAudioActive && LAudio.LAudioPlanRead(lSourcePath, LLibrarian.LLibrarianAudioLoad) is null)
        {
            return;
        }

        if (LAudio.LAudioPlanSave(lSourcePath, lPlan, LLibrarian.LLibrarianAudioSave))
        {
            QAudioInspector.QInspectorFailureSet(null);
        }
        else if (QAudioInspector.QInspectorFailureSet(lSourcePath))
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio edit not saved for '{
                    qAudioUsher.CUsherNameRead(lSourcePath)}': the sidecar could not be written",
                lSourcePath);
        }

        QAudioPersistentSave();
    }

    public void QAudioPlanRestore(string lSourcePath, bool lOwnerFirst)
    {
        bool lAdopted = false;
        QAudioInspector.QInspectorSaveSuspend();
        try
        {
            LWorkAudio? lSaved = LAudio.LAudioPlanRead(lSourcePath, LLibrarian.LLibrarianAudioLoad);
            LWorkAudio? lPersistent = qAudioSections.QInspectorPersistentCheck()
                ? qAudioSections.QInspectorPersistentRead()
                : null;
            if (lSaved is null && lPersistent is null && lOwnerFirst
                && QAudioPlanRead() is { LWorkAudioActive: true } lPending)
            {
                lSaved = lPending;
                lAdopted = true;
            }

            LWorkAudio lResolved = LAudio.LAudioPlanResolve(
                lSaved,
                lPersistent,
                QAudioInspector.QInspectorSkip.QSkipPersistent,
                QAudioInspector.QInspectorSkip.QSkipActive);
            qAudioSections.QInspectorAudioApply(lResolved);
            QAudioInspector.QInspectorSkip.QSkipActiveSet(lResolved.LWorkAudioSkip);
        }
        finally
        {
            QAudioInspector.QInspectorSaveResume();
        }

        QAudioProcessing.QProcessingSkipSet(QAudioInspector.QInspectorSkip.QSkipActive);
        QAudioActiveUpdate();
        QAudioViewerApply();
        if (lAdopted)
        {
            QAudioStateSave();
        }
    }

    public void QAudioActiveUpdate()
    {
        foreach (string lStep in QAudioProcessing.QProcessingSteps)
        {
            if (QAudioKindRead(lStep) is { } lKind)
            {
                QAudioProcessing.QProcessingActiveSet(lStep, qAudioSections.QInspectorStepRead(lKind).LWorkStepActive);
            }
        }
    }

    private void QAudioMediaHandle(LCargo lCargo)
    {
        bool lOwnerFirst = QAudioInspector.QInspectorOwnerSet(lCargo.LCargoSourcePath);
        QAudioPlanRestore(lCargo.LCargoSourcePath, lOwnerFirst);
        QAudioMonitor.QSMonitorSourceOpen(
            lCargo.LCargoSourcePath,
            lCargo.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero,
            QAudioRateRead());
        QAudioMonitor.QSMonitorPlanApply(QAudioPlanRead());
    }

    private void QAudioFanoutSave(IEnumerable<string> lPaths)
    {
        if (QAudioInspector.QInspectorSaveSuspended || !qAudioSections.QInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio lPersistent = qAudioSections.QInspectorPersistentRead();
        bool lSkipPersistent = QAudioInspector.QInspectorSkip.QSkipPersistent;
        bool lSkipApply = QAudioInspector.QInspectorSkip.QSkipActive;
        var lFailed = new List<string>();
        foreach (string lPath in lPaths)
        {
            bool lStored = LAudio.LAudioPlanSave(
                lPath,
                LAudio.LAudioPlanResolve(
                    LAudio.LAudioPlanRead(lPath, LLibrarian.LLibrarianAudioLoad),
                    lPersistent,
                    lSkipPersistent,
                    lSkipApply),
                LLibrarian.LLibrarianAudioSave);
            if (!lStored)
            {
                lFailed.Add(lPath);
            }
        }

        if (lFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio persistent save failed for {lFailed.Count} file(s): those sidecars were not written",
                string.Join(Environment.NewLine, lFailed));
        }
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
