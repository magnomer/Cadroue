using Cadroue.Core;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;
using Cadroue.Application;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
using Cadroue.Media;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PAudioTab
{
    private void PAudioPersistentRestore(LSceneTabRecord? lPreferenceTabLayout)
    {
        if (lPreferenceTabLayout?.LSceneInspector
            is not { LSceneInspectorAudio: { } pAudioPersistentRecord } pAudioInspector)
        {
            return;
        }

        pInspector.LInspector.LInspectorSaveSuspend();
        try
        {
            LWorkAudio pAudioPersistentPlan = LAudio.LAudioPersistentRead(pAudioPersistentRecord);
            pInspector.PInspectorPlanApply(pAudioPersistentPlan);
            pInspector.PInspectorPersistentApply(pAudioPersistentPlan, pAudioInspector.LSceneInspectorSkip);
            pInspector.PSkipApply(pAudioPersistentPlan.LWorkAudioSkip);
        }
        finally
        {
            pInspector.LInspector.LInspectorSaveResume();
        }
    }

    private void PAudioSkipHandle()
    {
        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioPlanSave();
    }

    private void PAudioPersistentSave()
    {
        if (pInspector.LInspector.LInspectorSaveSuspended || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        PAudioFanoutSave(pList.PListUnlockedRead().Select(pItem => pItem.LDocketEntryPath));
    }

    private void PAudioFanoutSave(IEnumerable<string> pAudioPaths)
    {
        LWorkAudio pAudioPersistent = pInspector.PInspectorPersistentRead();
        bool pAudioSkipPersistent = pInspector.PSkipPersistentCheck();
        bool pAudioSkipApply = pInspector.PSkipActiveCheck();
        var pAudioFailed = new List<string>();
        foreach (string pAudioPath in pAudioPaths)
        {
            bool pAudioStored = LAudio.LAudioPlanSave(
                pAudioPath,
                LAudio.LAudioPlanResolve(
                    LAudio.LAudioPlanRead(pAudioPath, LLibrarian.LLibrarianAudioLoad),
                    pAudioPersistent, pAudioSkipPersistent, pAudioSkipApply),
                LLibrarian.LLibrarianAudioSave);
            if (!pAudioStored)
            {
                pAudioFailed.Add(pAudioPath);
            }
        }

        if (pAudioFailed.Count > 0)
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio persistent save failed for {pAudioFailed.Count} file(s): those sidecars were not written",
                string.Join(Environment.NewLine, pAudioFailed));
        }
    }

    private void PAudioItemsHandle(IReadOnlyList<LDocketEntry> pAudioAddedItems)
    {
        if (pInspector.LInspector.LInspectorSaveSuspended || !pInspector.PInspectorPersistentCheck())
        {
            return;
        }

        PAudioFanoutSave(pAudioAddedItems.Select(pAudioAddedItem => pAudioAddedItem.LDocketEntryPath));
    }

    private void PAudioPathShow(string? pSourcePath)
    {
        if (!string.IsNullOrWhiteSpace(pSourcePath) && !pViewer.LViewer.LViewerSourceMatch(pSourcePath))
        {
            PAudioPlanSave();
            pViewer.PViewerSourceOpen(pSourcePath);
        }
    }

    private void PAudioMediaHandle(LCargo pMediaStatus)
    {
        bool pAudioOwnerFirst = pInspector.LInspector.LInspectorOwnerSet(pMediaStatus.LCargoSourcePath);
        PAudioPlanRestore(pMediaStatus.LCargoSourcePath, pAudioOwnerFirst);
        pAudioMonitor.LSMonitorSourceOpen(
            pMediaStatus.LCargoSourcePath,
            pMediaStatus.LCargoMediaInfo?.LMediaInfoDuration ?? TimeSpan.Zero,
            PAudioRateRead());
        pAudioMonitor.LSMonitorPlanApply(PAudioProcessingRead());
    }

    private void PAudioPlanRestore(string pSourcePath, bool pAudioOwnerFirst)
    {
        bool pAudioAdopted = false;
        pInspector.LInspector.LInspectorSaveSuspend();
        try
        {
            LWorkAudio? pSaved = LAudio.LAudioPlanRead(pSourcePath, LLibrarian.LLibrarianAudioLoad);
            LWorkAudio? pPersistent = pInspector.PInspectorPersistentCheck()
                ? pInspector.PInspectorPersistentRead()
                : null;
            if (pSaved is null && pPersistent is null && pAudioOwnerFirst
                && PAudioProcessingRead() is { LWorkAudioActive: true } pAudioPending)
            {
                pSaved = pAudioPending;
                pAudioAdopted = true;
            }

            LWorkAudio pResolved = LAudio.LAudioPlanResolve(
                pSaved, pPersistent, pInspector.PSkipPersistentCheck(), pInspector.PSkipActiveCheck());
            pInspector.PInspectorPlanApply(pResolved);
            pInspector.PSkipApply(pResolved.LWorkAudioSkip);
        }
        finally
        {
            pInspector.LInspector.LInspectorSaveResume();
        }

        pProcessing.PProcessingSkipSet(pInspector.PSkipActiveCheck());
        PAudioActiveUpdate();
        PAudioViewerApply();
        if (pAudioAdopted)
        {
            PAudioPlanSave();
        }
    }

    private void PAudioPlanSave()
    {
        if (pInspector.LInspector.LInspectorSaveSuspended
            || pInspector.LInspector.LInspectorOwnerPath is not { } pSourcePath
            || pList.PListLockCheck(pSourcePath))
        {
            return;
        }

        LWorkAudio pAudioPlan = PAudioProcessingRead();
        if (!pAudioPlan.LWorkAudioActive && LAudio.LAudioPlanRead(pSourcePath, LLibrarian.LLibrarianAudioLoad) is null)
        {
            return;
        }

        if (LAudio.LAudioPlanSave(pSourcePath, pAudioPlan, LLibrarian.LLibrarianAudioSave))
        {
            pInspector.LInspector.LInspectorFailureSet(null);
        }
        else if (pInspector.LInspector.LInspectorFailureSet(pSourcePath))
        {
            LTraceLog.LTraceWarningRecord(
                $"Audio edit not saved for '{System.IO.Path.GetFileName(pSourcePath)}': " +
                    "the sidecar could not be written",
                pSourcePath);
        }

        PAudioPersistentSave();
    }
}
