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
            LTraceLog.LTraceWarningRecord(
                $"Audio edit not saved for '{
                    qAudioUsher.CUsherNameRead(lSourcePath)}': the sidecar could not be written",
                lSourcePath);
        }

        QAudioPersistentSave();
    }

    public void QAudioPlanRestore(string lSourcePath, bool lOwnerFirst)
    {
        QInspector lInspector = qAudioTab.QAudioInspector;
        QInspectorAudio lSections = lInspector.QInspectorAudio;
        bool lAdopted = false;
        lInspector.QInspectorSaveSuspend();
        try
        {
            LWorkAudio? lSaved = LAudio.LAudioPlanRead(lSourcePath, LLibrarian.LLibrarianAudioLoad);
            LWorkAudio? lPersistent = lSections.QInspectorPersistentCheck()
                ? lSections.QInspectorPersistentRead()
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

    public void QAudioActiveUpdate()
    {
        foreach (string lStep in qAudioTab.QAudioProcessing.QProcessingSteps)
        {
            if (QAudioTab.QAudioKindRead(lStep) is { } lKind)
            {
                qAudioTab.QAudioProcessing.QProcessingActiveSet(
                    lStep, qAudioTab.QAudioInspector.QInspectorAudio.QInspectorStepRead(lKind).LWorkStepActive);
            }
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
}
