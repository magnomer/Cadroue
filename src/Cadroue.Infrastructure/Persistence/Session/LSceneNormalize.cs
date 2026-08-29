using System;
using System.Collections.Generic;
using System.Linq;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static partial class LScene
{
    public static LSceneRecord LSceneNormalize(LSceneRecord? lScene)
    {
        if (lScene is null)
        {
            return new LSceneRecord
            {
                LSceneVersion = LSceneVersionCurrent,
                LSceneDefaultTabs = true
            };
        }

        lScene.LSceneName = (lScene.LSceneName ?? string.Empty).Trim();
        lScene.LSceneLayoutKeys = LSceneListRead(lScene.LSceneLayoutKeys);
        lScene.LSceneTabNames = LSceneListRead(lScene.LSceneTabNames);
        lScene.LSceneTabExports = LSceneListRead(lScene.LSceneTabExports);
        lScene.LSceneTabLayouts = LSceneListRead(lScene.LSceneTabLayouts);
        lScene.LSceneTabRelays = LSceneListRead(lScene.LSceneTabRelays);

        int lSceneCount = lScene.LSceneLayoutKeys.Count;
        LSceneListNormalize(lScene.LSceneTabNames, lSceneCount, () => string.Empty);
        LSceneListNormalize(lScene.LSceneTabExports, lSceneCount, () => new LPresetRecord());
        LSceneListNormalize(lScene.LSceneTabLayouts, lSceneCount, () => new LSceneTabRecord());
        LSceneListNormalize(lScene.LSceneTabRelays, lSceneCount, () => -1);

        lScene.LSceneTabIndex = lSceneCount == 0
            ? 0
            : Math.Clamp(lScene.LSceneTabIndex, 0, lSceneCount - 1);

        foreach (LSceneTabRecord lSceneTab in lScene.LSceneTabLayouts)
        {
            LSceneTabNormalize(lSceneTab);
        }

        lScene.LSceneVersion = LSceneVersionCurrent;
        return lScene;
    }

    public static List<LSceneRecord> LSceneCatalogueNormalize(List<LSceneRecord>? lSceneCatalogue)
    {
        var lSceneSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lSceneNormalized = new List<LSceneRecord>();
        foreach (LSceneRecord lSceneRecord in lSceneCatalogue ?? new List<LSceneRecord>())
        {
            LSceneRecord lSceneReady = LSceneNormalize(lSceneRecord);
            if (lSceneReady.LSceneName.Length == 0 || !lSceneSeen.Add(lSceneReady.LSceneName))
            {
                continue;
            }

            lSceneNormalized.Add(lSceneReady);
        }

        return lSceneNormalized;
    }

    private static void LSceneTabNormalize(LSceneTabRecord lSceneTab)
    {
        lSceneTab.LScenePanelWidths = LSceneListRead(lSceneTab.LScenePanelWidths);
        lSceneTab.LScenePanelsCollapsed = LSceneListRead(lSceneTab.LScenePanelsCollapsed);
        lSceneTab.LSceneFunnelRules = LSceneListRead(lSceneTab.LSceneFunnelRules);
        lSceneTab.LSceneDetectors = LSceneListRead(lSceneTab.LSceneDetectors);
        LSceneInspectorNormalize(lSceneTab.LSceneInspector);
    }

    private static void LSceneInspectorNormalize(LSceneInspectorRecord? lSceneInspector)
    {
        if (lSceneInspector is null)
        {
            return;
        }

        if (lSceneInspector.LSceneInspectorEdit is { } lSceneEdit)
        {
            lSceneEdit.LSidecarSteps = LSceneListRead(lSceneEdit.LSidecarSteps);
        }

        if (lSceneInspector.LSceneInspectorAudio is { } lSceneAudio)
        {
            lSceneAudio.LSidecarSteps = LSceneListRead(lSceneAudio.LSidecarSteps);
            foreach (LSidecarAudioStep lSceneStep in lSceneAudio.LSidecarSteps)
            {
                lSceneStep.LSidecarEqualizerBands = LSceneListRead(lSceneStep.LSidecarEqualizerBands);
            }
        }

        if (lSceneInspector.LSceneInspectorFix is { } lSceneFix)
        {
            lSceneFix.LSidecarSteps = LSceneListRead(lSceneFix.LSidecarSteps);
        }
    }

    private static List<T> LSceneListRead<T>(List<T>? lSceneList) => lSceneList ?? new List<T>();

    private static void LSceneListNormalize<T>(List<T> lSceneList, int lSceneCount, Func<T> lSceneFill)
    {
        if (lSceneList.Count > lSceneCount)
        {
            lSceneList.RemoveRange(lSceneCount, lSceneList.Count - lSceneCount);
            return;
        }

        while (lSceneList.Count < lSceneCount)
        {
            lSceneList.Add(lSceneFill());
        }
    }
}
