using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed record LWindowTab(
    string LWindowTabKey,
    LPreset? LWindowTabPreset,
    LSceneTabEntry? LWindowTabLayout,
    string? LWindowTabName);

public sealed class LWindow
{
    private const double LWindowWidthFloor = 900;
    private static readonly string[] LWindowDefaultKeys = ["Split", "Edit", "Audio", "Convert", "Merge", "Worklist"];

    private readonly LStrip lStrip;
    private readonly Func<Func<bool>, bool> lWindowDispatcher;
    private LStripTab? lWindowTab;
    private LViewer? lWindowViewer;

    public CUsher LWindowUsher { get; }

    public LWindow(LStrip lStripOwner, Func<Func<bool>, bool> lDispatch)
    {
        lStrip = lStripOwner;
        LWindowUsher = lStripOwner.LStripUsher;
        lWindowDispatcher = lDispatch;
        LWindowDrop = new LWindowDrop(this);
        LWindowShortcut = new LWindowShortcut(this);
        LRelayChannel.LRelayAcceptSeam = LWindowRelayHandle;
    }

    public event Action<LWindowTab>? LWindowTabAdded;
    public event Action? LWindowFunnelUpdated;
    public event Action<LRelay>? LWindowRelayApplied;
    public event Action<double, double>? LWindowPlaced;
    public event Action? LWindowShown;
    public event Action<string>? LWindowMediaDeferred;
    public event Action<string>? LWindowMediaOpened;
    public event Action<LStripTab, double>? LWindowTabAttached;
    public event Action<LStripTab>? LWindowTabDetached;
    public event Action<bool>? LWindowLayoutChanged;
    public event Action? LWindowVerticalApplied;
    public event Action? LWindowHorizontalApplied;
    public event Action<double>? LWindowFlowApplied;
    public event Action<double>? LWindowVolumeSynced;

    public LWindowDrop LWindowDrop { get; }

    public LWindowShortcut LWindowShortcut { get; }

    public LStripTab? LWindowTab => lWindowTab;

    public LViewer? LWindowViewer => lWindowViewer;

    public bool LWindowListPresent => lWindowTab?.LStripTabDocket is not null;

    public bool LWindowViewerPresent => lWindowViewer is not null;

    public bool LWindowAudioAllowed => lWindowViewer?.LViewerAudioAllowed == true;

    public void LWindowTabSet(LStripTab? lTab, LViewer? lViewer)
    {
        if (lWindowTab is { } lPrevious)
        {
            LWindowTabDetached?.Invoke(lPrevious);
        }

        lWindowTab = lTab;
        lWindowViewer = lViewer;
        if (lTab is null)
        {
            return;
        }

        LWindowTabAttached?.Invoke(lTab, LFrameStore.LFrameStateCurrent.LFrameFlowHeight);
        LWindowVolumeSync(LPreference.LPreferenceStateCurrent);
    }

    public void LWindowTabsStart()
    {
        if (LRelayChannel.LRelayPayloadRead() is null)
        {
            LWindowStartupRestore(LPreference.LPreferenceStateCurrent, LScene.LSceneCurrent);
        }
    }

    public void LWindowRelayStart(double lScreenLeft, double lScreenTop, double lScreenWidth, double lScreenHeight)
    {
        if (LRelayChannel.LRelayPayloadRead() is { } lRelay)
        {
            (double lLeft, double lTop) = LSash.LSashRelayResolve(
                lRelay.LRelayDropLeft, lRelay.LRelayDropTop, lScreenLeft, lScreenTop, lScreenWidth, lScreenHeight);
            LWindowPlaced?.Invoke(lLeft, lTop);
            LWindowRelayAccept(lRelay);
            LRelayChannel.LRelayStartupCommit();
            return;
        }

        LWindowMediaRestore(LPreference.LPreferenceStateCurrent);
    }

    public void LWindowMediaRun(string lPath)
    {
        if (lWindowViewer is null)
        {
            LStripTab? lMediaTab = lStrip.LStripTabs.FirstOrDefault(lTab => !lTab.LStripTabWorklist);
            if (lMediaTab is not null)
            {
                lStrip.LStripSelect(lMediaTab);
            }
        }

        if (lWindowViewer is not null)
        {
            LWindowMediaOpened?.Invoke(lPath);
        }
    }

    public void LWindowOptionsApply()
    {
        LPreferenceState lPreference = LPreference.LPreferenceStateCurrent;
        LWindowLayoutChanged?.Invoke(lPreference.LPreferenceVerticalTabs);
        if (lPreference.LPreferenceVerticalTabs)
        {
            LWindowVerticalApplied?.Invoke();
        }
        else
        {
            LWindowHorizontalApplied?.Invoke();
        }

        LWindowFlowApplied?.Invoke(LFrameStore.LFrameStateCurrent.LFrameFlowHeight);
        LWindowVolumeSync(lPreference);
    }

    public (double LWindowMinimum, double LWindowWidth) LWindowWidthResolve(
        double? lRequired,
        double lReserved,
        double lWidth)
    {
        double lMinimum = Math.Max(LWindowWidthFloor, lRequired ?? 0) + lReserved;
        return (lMinimum, Math.Max(lWidth, lMinimum));
    }

    public void LWindowFrameSave(bool lNormal, LSashBounds lBounds, LSashBounds lRestore)
    {
        LSashBounds lFrame = lNormal ? lBounds : lRestore;
        if (lFrame.LSashWidth > 0 && lFrame.LSashHeight > 0)
        {
            LFrameStore.LFrameSave(new LFrameState
            {
                LFrameLeft = lFrame.LSashLeft,
                LFrameTop = lFrame.LSashTop,
                LFrameWidth = lFrame.LSashWidth,
                LFrameHeight = lFrame.LSashHeight,
                LFrameFlowHeight = LFrameStore.LFrameStateCurrent.LFrameFlowHeight
            });
        }
    }

    public void LWindowClose()
    {
        LScene.LSceneStateSave(LWindowSceneRead(LScene.LSceneActiveName));
        LRelayChannel.LRelayAcceptSeam = null;
    }

    public LSceneDocument LWindowSceneRead(string lSceneName) => new()
    {
        LSceneName = lSceneName,
        LSceneLayoutKeys = lStrip.LStripTabs.Select(lTab => lTab.LStripTabKey).ToList(),
        LSceneTabExports = lStrip.LStripTabs
            .Select(lTab => lTab.LStripTabPreset?.LPresetRecordCreate() ?? new LPresetDocument())
            .ToList(),
        LSceneTabLayouts = lStrip.LStripTabs
            .Select(lTab => lTab.LStripLayoutRead() ?? new LSceneTabEntry())
            .ToList(),
        LSceneTabRelays = LCartographer.LCartographerSlotResolve(LWindowIdsRead()).ToList(),
        LSceneTabNames = lStrip.LStripTabs.Select(lTab => lTab.LStripTabCustom).ToList(),
        LSceneTabIndex = lStrip.LStripSelected is null
            ? 0
            : Math.Max(0, lStrip.LStripTabs.ToList().IndexOf(lStrip.LStripSelected))
    };

    public bool LWindowSceneApply(LSceneDocument lScene, bool lConfirmed, Action lAllClose)
    {
        if (!lConfirmed)
        {
            return false;
        }

        LSceneDocument lBackup = LWindowSceneRead(LScene.LSceneActiveName);
        LTrace.LTraceLoadingSet(true);
        try
        {
            lAllClose();
            LWindowSceneRestore(lScene);
            LWindowLoadingRecord(lScene.LSceneName);
        }
        catch (Exception lSceneError)
        {
            LTraceLog.LTraceWarningRecord(
                "Scene apply failed; restoring the previous window",
                lSceneError.ToString());
            try
            {
                lAllClose();
                LWindowSceneRestore(lBackup);
            }
            catch
            {
                lAllClose();
                LWindowSceneRestore(LScene.LSceneNormalize(null));
            }
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }

        return true;
    }

    public void LWindowRelayAccept(LRelay lRelay)
    {
        LWindowTabAdded?.Invoke(new LWindowTab(
            lRelay.LRelayLayoutKey,
            LPreset.LPresetStateCreate(lRelay.LRelayExport),
            lRelay.LRelayLayout,
            lRelay.LRelayCustomName));
        LStripTab? lAdded = lStrip.LStripTabs.LastOrDefault();
        lStrip.LStripSelect(lAdded);
        LWindowRelayApplied?.Invoke(lRelay);
        LWindowFunnelUpdated?.Invoke();
    }

    private bool LWindowRelayHandle(LRelay lRelay)
    {
        try
        {
            return lWindowDispatcher(() =>
            {
                LWindowRelayAccept(lRelay);
                LWindowShown?.Invoke();
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

    private void LWindowStartupRestore(LPreferenceState lPreference, LSceneDocument lScene)
    {
        LTrace.LTraceLoadingSet(true);
        try
        {
            if (lPreference.LPreferenceStartupMode == "DefaultTab")
            {
                foreach (string lKey in lPreference.LPreferenceStartupTabs)
                {
                    LWindowTabAdded?.Invoke(new LWindowTab(lKey, null, null, null));
                }

                lStrip.LStripSelect(lStrip.LStripTabs.FirstOrDefault());
                LWindowLoadingRecord(null);
                return;
            }

            LWindowSceneRestore(lScene);
            LWindowLoadingRecord(lScene.LSceneName);
        }
        finally
        {
            LTrace.LTraceLoadingSet(false);
        }
    }

    private void LWindowSceneRestore(LSceneDocument lScene)
    {
        IReadOnlyList<string> lKeys = lScene.LSceneDefaultTabs ? LWindowDefaultKeys : lScene.LSceneLayoutKeys;
        lStrip.LStripUpdateSuspend();
        try
        {
            for (int lIndex = 0; lIndex < lKeys.Count; lIndex++)
            {
                LWindowTabAdded?.Invoke(new LWindowTab(
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
            lStrip.LStripUpdateResume();
        }

        LWindowSlotsApply(lScene.LSceneTabRelays);
        lStrip.LStripTitleUpdate();
        LWindowFunnelUpdated?.Invoke();
        if (lStrip.LStripTabs.Count == 0)
        {
            lStrip.LStripSelect(null);
            return;
        }

        int lSelectIndex = Math.Clamp(lScene.LSceneTabIndex, 0, lStrip.LStripTabs.Count - 1);
        lStrip.LStripSelect(lStrip.LStripTabs[lSelectIndex]);
    }

    private void LWindowSlotsApply(IReadOnlyList<int> lSlots)
    {
        foreach ((Guid lSource, Guid lTarget) in LCartographer.LCartographerAssignmentResolve(LWindowIdsRead(), lSlots))
        {
            if (lStrip.LStripTabFind(lSource) is null)
            {
                continue;
            }

            if (lTarget != LCartographer.LCartographerFinishTarget
                && lStrip.LStripTabFind(lTarget)?.LStripTabDocket is null)
            {
                continue;
            }

            LCartographer.LCartographerTargetSet(lSource, lTarget);
            lStrip.LStripTabFind(lSource)?.LStripTabAction?.LActionRelayApply(lTarget);
        }
    }

    private void LWindowLoadingRecord(string? lSceneName)
    {
        if (!string.IsNullOrWhiteSpace(lSceneName))
        {
            LTraceLog.LTraceLoadingRecord($"Scene \"{lSceneName}\" loaded");
        }

        int lCount = lStrip.LStripTabs.Count;
        string? lDetail = lCount == 0
            ? null
            : string.Join(
                Environment.NewLine,
                lStrip.LStripTabs.Select(lTab => $"Tab '{lTab.LStripTabTitle}' ({lTab.LStripTabKey}) opened"));
        LTraceLog.LTraceLoadingRecord($"{lCount} {(lCount == 1 ? "tab" : "tabs")} opened", lDetail);
    }

    private void LWindowMediaRestore(LPreferenceState lPreference)
    {
        if (!lPreference.LPreferenceMediaAutomatic
            || string.IsNullOrWhiteSpace(lPreference.LPreferenceMediaPath)
            || !LWindowUsher.CUsherFileExist(lPreference.LPreferenceMediaPath))
        {
            return;
        }

        LWindowMediaDeferred?.Invoke(lPreference.LPreferenceMediaPath);
    }

    private void LWindowVolumeSync(LPreferenceState lPreference)
    {
        if (lWindowViewer is null || !lPreference.LPreferenceVolumeUnified)
        {
            return;
        }

        LWindowVolumeSynced?.Invoke(lPreference.LPreferenceVolume);
    }

    private List<Guid> LWindowIdsRead() => lStrip.LStripTabs.Select(lTab => lTab.LStripTabId).ToList();
}
