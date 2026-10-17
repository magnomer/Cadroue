using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QAudioTabPlan
{
    private readonly QAudioTab qAudioTab;
    private readonly CUsher qAudioUsher;
    private readonly QList qAudioList;

    public QAudioTabPlan(QAudioTab lTab, CUsher lUsher, QList lList)
    {
        qAudioTab = lTab;
        qAudioUsher = lUsher;
        qAudioList = lList;
    }

    public void QAudioItemsHandle(IReadOnlyList<LDocketEntry> lAdded) =>
        QAudioFanoutSave(lAdded.Select(lItem => lItem.LDocketEntryPath));

    public void QAudioPersistentSave() =>
        QAudioFanoutSave(qAudioList.QListDocket.LDocketUnlockedRead().Select(lItem => lItem.LDocketEntryPath));

    public LWorkAudio QAudioPlanRead() =>
        qAudioTab.QAudioInspector.QInspectorAudio.QInspectorAudioRead(
            qAudioTab.QAudioProcessing.QProcessingSteps.Select(QAudioTab.QAudioKindRead).OfType<LAudioKind>());

    public void QAudioStateSave()
    {
        QInspector lInspector = qAudioTab.QAudioInspector;
        if (lInspector.QInspectorSaveSuspended
            || lInspector.QInspectorOwnerPath is not { } lSourcePath
            || qAudioList.QListDocket.LDocketLockCheck(lSourcePath))
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
            lInspector.QInspectorFailureSet(null);
        }
        else if (lInspector.QInspectorFailureSet(lSourcePath))
        {
            QAudioFailureRecord(lSourcePath);
        }

        QAudioPersistentSave();
    }

    private void QAudioFailureRecord(string lSourcePath) =>
        LTraceLog.LTraceWarningRecord(
            $"Audio edit not saved for '{
                qAudioUsher.CUsherNameRead(lSourcePath)}': the sidecar could not be written",
            lSourcePath);

    public void QAudioPlanRestore(string lSourcePath, bool lOwnerFirst)
    {
        QInspector lInspector = qAudioTab.QAudioInspector;
        QInspectorAudio lSections = lInspector.QInspectorAudio;
        bool lAdopted = false;
        lInspector.QInspectorSaveSuspend();
        try
        {
            LWorkAudio? lSaved = LAudio.LAudioPlanRead(lSourcePath, LLibrarian.LLibrarianAudioLoad);
            LWorkAudio? lPersistent = QAudioPersistentRead(lSections);
            (lSaved, lAdopted) = QAudioSavedResolve(lSaved, lPersistent, lOwnerFirst);
            LWorkAudio lResolved = LAudio.LAudioPlanResolve(
                lSaved,
                lPersistent,
                lInspector.QInspectorSkip.QSkipPersistent,
                lInspector.QInspectorSkip.QSkipActive);
            lSections.QInspectorAudioApply(lResolved);
            lInspector.QInspectorSkip.QSkipActiveSet(lResolved.LWorkAudioSkip);
        }
        finally
        {
            lInspector.QInspectorSaveResume();
        }

        qAudioTab.QAudioProcessing.QProcessingSkipSet(lInspector.QInspectorSkip.QSkipActive);
        QAudioActiveUpdate();
        qAudioTab.QAudioViewerApply();
        if (lAdopted)
        {
            QAudioStateSave();
        }
    }

    public void QAudioActiveUpdate() => QAudioActiveApply(QAudioActiveRead());

    private static LWorkAudio? QAudioPersistentRead(QInspectorAudio lSections) =>
        lSections.QInspectorPersistentCheck()
            ? lSections.QInspectorPersistentRead()
            : null;

    // With no saved and no persistent plan, the owner's first visit adopts the pending active plan.
    private (LWorkAudio? QAudioSaved, bool QAudioAdopted) QAudioSavedResolve(
        LWorkAudio? lSaved,
        LWorkAudio? lPersistent,
        bool lOwnerFirst) =>
        lSaved is null && lPersistent is null && lOwnerFirst
        && QAudioPlanRead() is { LWorkAudioActive: true } lPending
            ? (lPending, true)
            : (lSaved, false);

    private IReadOnlyList<(string QAudioStep, bool QAudioActive)> QAudioActiveRead()
    {
        var lStates = new List<(string QAudioStep, bool QAudioActive)>();
        foreach (string lStep in qAudioTab.QAudioProcessing.QProcessingSteps)
        {
            if (QAudioTab.QAudioKindRead(lStep) is { } lKind)
            {
                lStates.Add(
                    (lStep, qAudioTab.QAudioInspector.QInspectorAudio.QInspectorStepRead(lKind).LWorkStepActive));
            }
        }

        return lStates;
    }

    private void QAudioActiveApply(IReadOnlyList<(string QAudioStep, bool QAudioActive)> lStates)
    {
        foreach ((string lStep, bool lActive) in lStates)
        {
            qAudioTab.QAudioProcessing.QProcessingActiveSet(lStep, lActive);
        }
    }

    private void QAudioFanoutSave(IEnumerable<string> lPaths)
    {
        QInspector lInspector = qAudioTab.QAudioInspector;
        QInspectorAudio lSections = lInspector.QInspectorAudio;
        if (lInspector.QInspectorSaveSuspended || !lSections.QInspectorPersistentCheck())
        {
            return;
        }

        LWorkAudio lPersistent = lSections.QInspectorPersistentRead();
        bool lSkipPersistent = lInspector.QInspectorSkip.QSkipPersistent;
        bool lSkipApply = lInspector.QInspectorSkip.QSkipActive;
        IReadOnlyList<string> lFailed = QAudioPathsSave(lPaths, lPersistent, lSkipPersistent, lSkipApply);
        if (lFailed.Count > 0)
        {
            QAudioFanoutRecord(lFailed);
        }
    }

    private static void QAudioFanoutRecord(IReadOnlyList<string> lFailed) =>
        LTraceLog.LTraceWarningRecord(
            $"Audio persistent save failed for {lFailed.Count} file(s): those sidecars were not written",
            string.Join(Environment.NewLine, lFailed));

    private static IReadOnlyList<string> QAudioPathsSave(
        IEnumerable<string> lPaths,
        LWorkAudio lPersistent,
        bool lSkipPersistent,
        bool lSkipApply)
    {
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

        return lFailed;
    }
}
