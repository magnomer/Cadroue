using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class QWindowScene
{
    private static readonly string[] QWindowDefaultKeys = ["Split", "Edit", "Audio", "Convert", "Merge", "Worklist"];

    private readonly QStrip qStrip;

    public QWindowScene(QStrip lStrip)
    {
        qStrip = lStrip;
    }

    public event Action<QWindowTab>? QWindowSceneAdded;
    public event Action? QWindowSceneRestored;

    public LSceneDocument QWindowSceneRead(string lSceneName) => new()
    {
        LSceneName = lSceneName,
        LSceneLayoutKeys = qStrip.QStripTabs.Select(lTab => lTab.QStripTabKey).ToList(),
        LSceneTabExports = qStrip.QStripTabs
            .Select(lTab => lTab.QStripTabPreset?.LPresetRecordCreate() ?? new LPresetDocument())
            .ToList(),
        LSceneTabLayouts = qStrip.QStripTabs
            .Select(lTab => lTab.QStripLayoutRead() ?? new LSceneTabEntry())
            .ToList(),
        LSceneTabRelays = LCartographer.LCartographerSlotResolve(QWindowIdsRead()).ToList(),
        LSceneTabNames = qStrip.QStripTabs.Select(lTab => lTab.QStripTabCustom).ToList(),
        LSceneTabIndex = qStrip.QStripSelected is null
            ? 0
            : Math.Max(0, qStrip.QStripTabs.ToList().IndexOf(qStrip.QStripSelected))
    };

    public bool QWindowSceneApply(LSceneDocument lScene, bool lConfirmed, Action lAllClose)
    {
        if (!lConfirmed)
        {
            return false;
        }

        LSceneDocument lBackup = QWindowSceneRead(LScene.LSceneActiveName);
        LTrace.LTraceLoadingSet(true);
        try
        {
            lAllClose();
            QWindowSceneRestore(lScene);
            QWindowLoadingRecord(lScene.LSceneName);
        }
        catch (Exception lSceneError)
        {
            LTraceLog.LTraceWarningRecord(
                "Scene apply failed; restoring the previous window",
                lSceneError.ToString());
            try
            {
                lAllClose();
                QWindowSceneRestore(lBackup);
            }
            catch
            {
                lAllClose();
                QWindowSceneRestore(LScene.LSceneNormalize(null));
            }
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }

        return true;
    }

    internal void QWindowSceneRestore(LSceneDocument lScene)
    {
        IReadOnlyList<string> lKeys = lScene.LSceneDefaultTabs ? QWindowDefaultKeys : lScene.LSceneLayoutKeys;
        qStrip.QStripUpdateSuspend();
        try
        {
            for (int lIndex = 0; lIndex < lKeys.Count; lIndex++)
            {
                QWindowSceneAdded?.Invoke(new QWindowTab(
                    lKeys[lIndex],
                    lIndex < lScene.LSceneTabExports.Count
                        ? LPreset.LPresetStateCreate(lScene.LSceneTabExports[lIndex])
                        : null,
                    lIndex < lScene.LSceneTabLayouts.Count ? lScene.LSceneTabLayouts[lIndex] : null,
                    lIndex < lScene.LSceneTabNames.Count ? lScene.LSceneTabNames[lIndex] : null));
            }
        }
        finally
        {
            qStrip.QStripUpdateResume();
        }

        QWindowSlotsApply(lScene.LSceneTabRelays);
        qStrip.QStripTitleUpdate();
        QWindowSceneRestored?.Invoke();
        if (qStrip.QStripTabs.Count == 0)
        {
            qStrip.QStripSelect(null);
            return;
        }

        int lSelectIndex = Math.Clamp(lScene.LSceneTabIndex, 0, qStrip.QStripTabs.Count - 1);
        qStrip.QStripSelect(qStrip.QStripTabs[lSelectIndex]);
    }

    private void QWindowSlotsApply(IReadOnlyList<int> lSlots)
    {
        foreach ((Guid lSource, Guid lTarget) in LCartographer.LCartographerAssignmentResolve(QWindowIdsRead(), lSlots))
        {
            if (qStrip.QStripTabFind(lSource) is null)
            {
                continue;
            }

            if (lTarget != LCartographer.LCartographerFinishTarget
                && qStrip.QStripTabFind(lTarget)?.QStripTabDocket is null)
            {
                continue;
            }

            LCartographer.LCartographerTargetSet(lSource, lTarget);
            qStrip.QStripTabFind(lSource)?.QStripTabAction?.QActionRelayApply(lTarget);
        }
    }

    internal void QWindowLoadingRecord(string? lSceneName)
    {
        if (!string.IsNullOrWhiteSpace(lSceneName))
        {
            LTraceLog.LTraceLoadingRecord($"Scene \"{lSceneName}\" loaded");
        }

        int lCount = qStrip.QStripTabs.Count;
        string? lDetail = lCount == 0
            ? null
            : string.Join(
                Environment.NewLine,
                qStrip.QStripTabs.Select(lTab => $"Tab '{lTab.QStripTabTitle}' ({lTab.QStripTabKey}) opened"));
        LTraceLog.LTraceLoadingRecord($"{lCount} {(lCount == 1 ? "tab" : "tabs")} opened", lDetail);
    }

    private List<Guid> QWindowIdsRead() => qStrip.QStripTabs.Select(lTab => lTab.QStripTabId).ToList();
}
