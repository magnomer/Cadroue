using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record QWindowTab(
    string QWindowTabKey,
    LPreset? QWindowTabPreset,
    LSceneTabEntry? QWindowTabLayout,
    string? QWindowTabName);

public sealed class QWindow
{
    private const double QWindowWidthFloor = 900;
    private static readonly string[] QWindowDefaultKeys = ["Split", "Edit", "Audio", "Convert", "Merge", "Worklist"];

    private readonly QStrip qStrip;
    private QStripTab? qWindowTab;
    private QViewer? qWindowViewer;

    internal CUsher QWindowUsher { get; }

    public QWindow(CAtelier lAtelier)
    {
        ArgumentNullException.ThrowIfNull(lAtelier);

        QWindowUsher = lAtelier.CAtelierUsher;
        qStrip = new QStrip(QWindowUsher, lAtelier.CAtelierLineage);
        QWindowLogo = new QLogoMenu(QWindowUsher);
        QWindowPart = new QWindowPart(this);
    }

    public QStrip QWindowStrip => qStrip;

    public QWorkspacePair QWindowPairCreate() => new(new QWorkspaceFacts(string.Empty), QWindowUsher);

    public QLogoMenu QWindowLogo { get; }

    public void QWindowDispatcherAttach(Func<Func<bool>, bool> lDispatch) =>
        LRelayChannel.LRelayAcceptSeam = lRelay => QWindowRelayHandle(lRelay, lDispatch);

    public QStripTab QWindowTabCreate(QWindowTab lTab)
    {
        QStripTab lStripTab = qStrip.QStripTabCreate(lTab.QWindowTabKey);
        if (lTab.QWindowTabPreset is { } lPreset)
        {
            lStripTab.QStripTabWorkspace.QWorkspacePresetAccept(lPreset);
        }

        return lStripTab;
    }

    public event Action<QWindowTab>? QWindowTabAdded;
    public event Action? QWindowFunnelUpdated;
    public event Action<LRelay>? QWindowRelayApplied;
    public event Action<double, double>? QWindowPlaced;
    public event Action? QWindowShown;
    public event Action<string>? QWindowMediaDeferred;
    public event Action<string>? QWindowMediaOpened;
    public event Action<QStripTab, double>? QWindowTabAttached;
    public event Action<QStripTab>? QWindowTabDetached;
    public event Action<bool>? QWindowLayoutChanged;
    public event Action? QWindowVerticalApplied;
    public event Action? QWindowHorizontalApplied;
    public event Action<double>? QWindowFlowApplied;
    public event Action<double>? QWindowVolumeSynced;

    public QWindowPart QWindowPart { get; }

    public QStripTab? QWindowTab => qWindowTab;

    public QViewer? QWindowViewer => qWindowViewer;

    public bool QWindowListPresent => qWindowTab?.QStripTabDocket is not null;

    public bool QWindowViewerPresent => qWindowViewer is not null;

    public bool QWindowAudioAllowed => qWindowViewer?.QViewerAudioAllowed == true;

    public void QWindowTabSet(QStripTab? lTab, QViewer? lViewer)
    {
        if (qWindowTab is { } lPrevious)
        {
            QWindowTabDetached?.Invoke(lPrevious);
        }

        qWindowTab = lTab;
        qWindowViewer = lViewer;
        if (lTab is null)
        {
            return;
        }

        QWindowTabAttached?.Invoke(lTab, LFrameStore.LFrameStateCurrent.LFrameFlowHeight);
        QWindowVolumeSync(LPreference.LPreferenceStateCurrent);
    }

    public void QWindowTabsStart()
    {
        if (LRelayChannel.LRelayPayloadRead() is null)
        {
            QWindowStartupRestore(LPreference.LPreferenceStateCurrent, LScene.LSceneCurrent);
        }
    }

    public void QWindowRelayStart(double lScreenLeft, double lScreenTop, double lScreenWidth, double lScreenHeight)
    {
        if (LRelayChannel.LRelayPayloadRead() is { } lRelay)
        {
            (double lLeft, double lTop) = QSash.QSashRelayResolve(
                lRelay.LRelayDropLeft, lRelay.LRelayDropTop, lScreenLeft, lScreenTop, lScreenWidth, lScreenHeight);
            QWindowPlaced?.Invoke(lLeft, lTop);
            QWindowRelayAccept(lRelay);
            LRelayChannel.LRelayStartupCommit();
            return;
        }

        QWindowMediaRestore(LPreference.LPreferenceStateCurrent);
    }

    public void QWindowMediaRun(string lPath)
    {
        if (qWindowViewer is null)
        {
            QStripTab? lMediaTab = qStrip.QStripTabs.FirstOrDefault(lTab => !lTab.QStripTabWorklist);
            if (lMediaTab is not null)
            {
                qStrip.QStripSelect(lMediaTab);
            }
        }

        if (qWindowViewer is not null)
        {
            QWindowMediaOpened?.Invoke(lPath);
        }
    }

    public void QWindowOptionsApply()
    {
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent;
        QWindowLayoutChanged?.Invoke(lPreference.LPreferenceVerticalTabs);
        if (lPreference.LPreferenceVerticalTabs)
        {
            QWindowVerticalApplied?.Invoke();
        }
        else
        {
            QWindowHorizontalApplied?.Invoke();
        }

        QWindowFlowApplied?.Invoke(LFrameStore.LFrameStateCurrent.LFrameFlowHeight);
        QWindowVolumeSync(lPreference);
    }

    public (double QWindowMinimum, double QWindowWidth) QWindowWidthResolve(
        double? lRequired,
        double lReserved,
        double lWidth)
    {
        double lMinimum = Math.Max(QWindowWidthFloor, lRequired ?? 0) + lReserved;
        return (lMinimum, Math.Max(lWidth, lMinimum));
    }

    public void QWindowFrameSave(bool lNormal, QSashBounds lBounds, QSashBounds lRestore)
    {
        QSashBounds lFrame = lNormal ? lBounds : lRestore;
        if (lFrame.QSashWidth > 0 && lFrame.QSashHeight > 0)
        {
            LFrameStore.LFrameSave(new LFrameState
            {
                LFrameLeft = lFrame.QSashLeft,
                LFrameTop = lFrame.QSashTop,
                LFrameWidth = lFrame.QSashWidth,
                LFrameHeight = lFrame.QSashHeight,
                LFrameFlowHeight = LFrameStore.LFrameStateCurrent.LFrameFlowHeight
            });
        }
    }

    public void QWindowClose()
    {
        LScene.LSceneStateSave(QWindowSceneRead(LScene.LSceneActiveName));
        LRelayChannel.LRelayAcceptSeam = null;
    }

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

    public void QWindowRelayAccept(LRelay lRelay)
    {
        QWindowTabAdded?.Invoke(new QWindowTab(
            lRelay.LRelayLayoutKey,
            LPreset.LPresetStateCreate(lRelay.LRelayExport),
            lRelay.LRelayLayout,
            lRelay.LRelayCustomName));
        QStripTab? lAdded = qStrip.QStripTabs.LastOrDefault();
        qStrip.QStripSelect(lAdded);
        QWindowRelayApplied?.Invoke(lRelay);
        QWindowFunnelUpdated?.Invoke();
    }

    private bool QWindowRelayHandle(LRelay lRelay, Func<Func<bool>, bool> lDispatch)
    {
        try
        {
            return lDispatch(() =>
            {
                QWindowRelayAccept(lRelay);
                QWindowShown?.Invoke();
                return true;
            });
        }
        catch (Exception lRelayException)
        {
            LTraceLog.LTraceErrorRecord(
                $"Relayed '{lRelay.LRelayLayoutKey}' tab could not be taken; refused",
                lRelayException);
            return false;
        }
    }

    private void QWindowStartupRestore(LPreferenceState lPreference, LSceneDocument lScene)
    {
        LTrace.LTraceLoadingSet(true);
        try
        {
            if (lPreference.LPreferenceStartupMode == "DefaultTab")
            {
                foreach (string lKey in lPreference.LPreferenceStartupTabs)
                {
                    QWindowTabAdded?.Invoke(new QWindowTab(lKey, null, null, null));
                }

                qStrip.QStripSelect(qStrip.QStripTabs.FirstOrDefault());
                QWindowLoadingRecord(null);
                return;
            }

            QWindowSceneRestore(lScene);
            QWindowLoadingRecord(lScene.LSceneName);
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }
    }

    private void QWindowSceneRestore(LSceneDocument lScene)
    {
        IReadOnlyList<string> lKeys = lScene.LSceneDefaultTabs ? QWindowDefaultKeys : lScene.LSceneLayoutKeys;
        qStrip.QStripUpdateSuspend();
        try
        {
            for (int lIndex = 0; lIndex < lKeys.Count; lIndex++)
            {
                QWindowTabAdded?.Invoke(new QWindowTab(
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
        QWindowFunnelUpdated?.Invoke();
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

    private void QWindowLoadingRecord(string? lSceneName)
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

    private void QWindowMediaRestore(LPreferenceState lPreference)
    {
        if (!lPreference.LPreferenceMediaAutomatic
            || string.IsNullOrWhiteSpace(lPreference.LPreferenceMediaPath)
            || !QWindowUsher.CUsherFileExist(lPreference.LPreferenceMediaPath))
        {
            return;
        }

        QWindowMediaDeferred?.Invoke(lPreference.LPreferenceMediaPath);
    }

    private void QWindowVolumeSync(LPreferenceState lPreference)
    {
        if (qWindowViewer is null || !lPreference.LPreferenceVolumeUnified)
        {
            return;
        }

        QWindowVolumeSynced?.Invoke(lPreference.LPreferenceVolume);
    }

    private List<Guid> QWindowIdsRead() => qStrip.QStripTabs.Select(lTab => lTab.QStripTabId).ToList();
}
