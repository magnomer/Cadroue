using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LSceneCanonical
{
    public const int LSceneVersionCurrent = 1;

    public static LSceneDocument LSceneNormalize(LSceneDocument? lScene)
    {
        if (lScene is null)
        {
            return new LSceneDocument
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
        LSceneListNormalize(lScene.LSceneTabExports, lSceneCount, () => new LPresetDocument());
        LSceneListNormalize(lScene.LSceneTabLayouts, lSceneCount, () => new LSceneTabEntry());
        LSceneListNormalize(lScene.LSceneTabRelays, lSceneCount, () => -1);

        lScene.LSceneTabIndex = lSceneCount == 0
            ? 0
            : Math.Clamp(lScene.LSceneTabIndex, 0, lSceneCount - 1);

        foreach (LSceneTabEntry lSceneTab in lScene.LSceneTabLayouts)
        {
            LSceneTabNormalize(lSceneTab);
        }

        lScene.LSceneVersion = LSceneVersionCurrent;
        return lScene;
    }

    public static bool LSceneMatch(LSceneDocument lSceneLeft, LSceneDocument lSceneRight) =>
        string.Equals(LSceneCanonicalRead(lSceneLeft), LSceneCanonicalRead(lSceneRight), StringComparison.Ordinal);

    private static string LSceneCanonicalRead(LSceneDocument lScene)
    {
        var lSceneCanonical = new LSceneDocument
        {
            LSceneLayoutKeys = lScene.LSceneLayoutKeys,
            LSceneTabNames = lScene.LSceneTabNames,
            LSceneTabExports = lScene.LSceneTabExports,
            LSceneTabLayouts = lScene.LSceneTabLayouts
                .Select(lSceneTab =>
                {
                    LSceneTabEntry lSceneTabCanonical = lSceneTab.LSceneTabClone();
                    lSceneTabCanonical.LScenePanelWidths =
                        LSceneWidthsRead(lSceneTabCanonical.LScenePanelWidths);
                    return lSceneTabCanonical;
                })
                .ToList(),
            LSceneTabRelays = lScene.LSceneTabRelays
        };

        return JsonSerializer.Serialize(lSceneCanonical);
    }

    private static List<double> LSceneWidthsRead(IReadOnlyList<double> lScenePanelWidths)
    {
        double lSceneWidthTotal = lScenePanelWidths.Sum(lSceneWidth => Math.Max(0, lSceneWidth));
        if (lSceneWidthTotal <= 0)
        {
            return lScenePanelWidths.ToList();
        }

        return lScenePanelWidths
            .Select(lSceneWidth => Math.Round(Math.Max(0, lSceneWidth) / lSceneWidthTotal, 12))
            .ToList();
    }

    private static void LSceneTabNormalize(LSceneTabEntry lSceneTab)
    {
        lSceneTab.LScenePanelWidths = LSceneListRead(lSceneTab.LScenePanelWidths);
        lSceneTab.LScenePanelsCollapsed = LSceneListRead(lSceneTab.LScenePanelsCollapsed);
        lSceneTab.LSceneFunnelRules = LSceneListRead(lSceneTab.LSceneFunnelRules);
        lSceneTab.LSceneDetectors = LSceneListRead(lSceneTab.LSceneDetectors);
        LSceneInspectorNormalize(lSceneTab.LSceneInspector);
    }

    private static void LSceneInspectorNormalize(LSceneInspectorEntry? lSceneInspector)
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

    private static List<LSceneElement> LSceneListRead<LSceneElement>(List<LSceneElement>? lSceneList) =>
        lSceneList ?? new List<LSceneElement>();

    private static void LSceneListNormalize<LSceneElement>(
        List<LSceneElement> lSceneList,
        int lSceneCount,
        Func<LSceneElement> lSceneFill)
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
