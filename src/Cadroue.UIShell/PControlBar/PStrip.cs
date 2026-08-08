using Cadroue.Core;
using System.Collections.ObjectModel;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainArea;
using Cadroue.UIShell.PPanels;
using Cadroue.Application;
using Cadroue.ShellEngine;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PControlBar;

public sealed class PStrip
{
    private const string pStripSplitIcon = "/PAssets/PTabs/PSplitButton.svg";
    private const string pStripEditIcon = "/PAssets/PTabs/PEditButton.svg";
    private const string pStripAudioIcon = "/PAssets/PTabs/PAudioButton.svg";
    private const string pStripConvertIcon = "/PAssets/PTabs/PConvertButton.svg";
    private const string pStripMergeIcon = "/PAssets/PTabs/PMergeButton.svg";
    private const string pStripFunnelIcon = "/PAssets/PTabs/PFunnelButton.svg";
    private const string pStripWorklistIcon = "/PAssets/PTabs/PWorklistButton.svg";

    private PTabRecord? pStripSelected;
    private PTabRecord? pStripHovered;
    private bool pStripUpdateSuspended;

    public PStrip()
    {
        PStripRecords = new ObservableCollection<PTabRecord>();
        PStripCurrent = this;
    }

    public static PStrip? PStripCurrent { get; private set; }

    public ObservableCollection<PTabRecord> PStripRecords { get; }

    public static string PStripTitleRead(Guid pStripTabId) =>
        PStripCurrent?.PStripRecords.FirstOrDefault(pTabItem => pTabItem.PTabId == pStripTabId)?.PTabTitle
        ?? LCartographer.LCartographerStageRead(pStripTabId);

    public PTabRecord? PStripSelected
    {
        get => pStripSelected;
        private set
        {
            if (ReferenceEquals(pStripSelected, value))
            {
                return;
            }

            pStripSelected = value;
            PStripSelectChange?.Invoke(pStripSelected);
        }
    }

    public event Action<PTabRecord?>? PStripSelectChange;

    public void PStripUpdateSuspend() => pStripUpdateSuspended = true;

    public void PStripUpdateResume() => pStripUpdateSuspended = false;

    private IReadOnlyList<LTabsetSlot> PStripSlotsRead() =>
        PStripRecords
            .Select(pTabItem => new LTabsetSlot(
                pTabItem.PTabId, pTabItem.PTabLayoutKey, pTabItem.PTabNameCustom, pTabItem.PTabOrdinal))
            .ToList();

    private void PStripSeparatorUpdate()
    {
        var selectedIndex = PStripSelected is null ? -1 : PStripRecords.IndexOf(PStripSelected);
        var hoveredIndex = pStripHovered is null ? -1 : PStripRecords.IndexOf(pStripHovered);
        for (var i = 0; i < PStripRecords.Count; i++)
        {
            PStripRecords[i].PTabSeparatorState =
                i < PStripRecords.Count - 1
                && i != selectedIndex
                && i != selectedIndex - 1
                && i != hoveredIndex
                && i != hoveredIndex - 1;
        }
    }

    internal void PStripHoverSet(PTabRecord pTabRecord)
    {
        if (ReferenceEquals(pStripHovered, pTabRecord))
        {
            return;
        }

        pStripHovered = pTabRecord;
        PStripSeparatorUpdate();
    }

    internal void PStripHoverClear(PTabRecord pTabRecord)
    {
        if (!ReferenceEquals(pStripHovered, pTabRecord))
        {
            return;
        }

        pStripHovered = null;
        PStripSeparatorUpdate();
    }

    internal void PStripHoverClear()
    {
        if (pStripHovered is null)
        {
            return;
        }

        pStripHovered = null;
        PStripSeparatorUpdate();
    }

    public PTabRecord PStripAdd()
    {
        return PStripAdd("Split");
    }

    public PTabRecord PStripAdd(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LSceneTabRecord? lPreferenceTabLayout = null)
    {
        return pTabLayoutKey switch
        {
            "Edit" => PStripTypedAdd("Edit", pStripEditIcon, lExportSpecificState, lPreferenceTabLayout),
            "Audio" => PStripTypedAdd("Audio", pStripAudioIcon, lExportSpecificState, lPreferenceTabLayout),
            "Convert" => PStripTypedAdd("Convert", pStripConvertIcon, lExportSpecificState, lPreferenceTabLayout),
            "Merge" => PStripTypedAdd("Merge", pStripMergeIcon, lExportSpecificState, lPreferenceTabLayout),
            "Funnel" => PStripTypedAdd("Funnel", pStripFunnelIcon, lExportSpecificState, lPreferenceTabLayout),
            "Worklist" => PStripTypedAdd("Worklist", pStripWorklistIcon, lExportSpecificState, lPreferenceTabLayout),
            _ => PStripTypedAdd("Split", pStripSplitIcon, lExportSpecificState, lPreferenceTabLayout)
        };
    }

    private PTabRecord PStripTypedAdd(
        string pTabLayoutKey,
        string pTabIconPath,
        LPreset? lExportSpecificState,
        LSceneTabRecord? lPreferenceTabLayout)
    {
        var pTabRecord = new PTabRecord(
            PStripTitleRead(pTabLayoutKey),
            pTabLayoutKey,
            PIcon.PIconRead(pTabIconPath),
            lExportSpecificState,
            lPreferenceTabLayout)
        {
            PTabOrdinal = LTabset.LTabsetOrdinalRead(PStripSlotsRead(), pTabLayoutKey)
        };
        PStripRecords.Add(pTabRecord);
        LTraceLog.LTraceInfoRecord(
            $"Tab opened '{pTabRecord.PTabTitle}' ({pTabLayoutKey}): {PStripRecords.Count} tab(s) open");

        if (pStripUpdateSuspended)
        {
            return pTabRecord;
        }

        PStripTitleUpdate();

        if (PStripSelected is null)
        {
            PStripSelect(pTabRecord);
        }
        else
        {
            PStripSeparatorUpdate();
        }

        return pTabRecord;
    }

    public void PStripTitleUpdate()
    {
        if (pStripUpdateSuspended)
        {
            return;
        }

        IReadOnlyList<LTabsetTitlePlan> pStripPlans = LTabset.LTabsetTitlePlan(PStripSlotsRead());
        Dictionary<Guid, PTabRecord> pStripById = PStripRecords.ToDictionary(pTabItem => pTabItem.PTabId);
        foreach (LTabsetTitlePlan pStripPlan in pStripPlans)
        {
            if (!pStripById.TryGetValue(pStripPlan.LTabsetId, out PTabRecord? pTabRecord))
            {
                continue;
            }

            if (pStripPlan.LTabsetCustom)
            {
                pTabRecord.PTabTitle = pTabRecord.PTabNameCustom;
                continue;
            }

            pTabRecord.PTabOrdinal = pStripPlan.LTabsetOrdinal;
            pTabRecord.PTabTitle = pStripPlan.LTabsetNumbered
                ? LLocalization.LLocalizationFormat(
                    "Tab.Numbered", PStripTitleRead(pTabRecord.PTabLayoutKey), pStripPlan.LTabsetOrdinal)
                : PStripTitleRead(pTabRecord.PTabLayoutKey);
        }

        PStripRelayUpdate();
    }

    public static void PStripRelayUpdate()
    {
        if (PStripCurrent is not { } pStripTabset)
        {
            return;
        }

        foreach (PTabRecord pTabRecord in pStripTabset.PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabAction?.PActionRelayApply(
                LCartographer.LCartographerTargetRead(pTabRecord.PTabId));
        }
    }

    public static IReadOnlyList<PActionRelayOption> PStripRelayRead(Guid pStripSourceTab)
    {
        if (PStripCurrent is not { } pStripTabset)
        {
            return Array.Empty<PActionRelayOption>();
        }

        var pStripOptions = new List<PActionRelayOption>();
        foreach (PTabRecord pTabRecord in pStripTabset.PStripRecords)
        {
            if (pTabRecord.PTabId == pStripSourceTab
                || pTabRecord.PTabWorkspace.PWorkspaceSurface.PTabList is null)
            {
                continue;
            }

            pStripOptions.Add(new PActionRelayOption(
                pTabRecord.PTabId, pTabRecord.PTabTitle, pTabRecord.PTabIconSource));
        }

        return pStripOptions;
    }

    public static PTabRecord? PStripTabFind(LWorkItem lWorkItem)
    {
        Guid pStripSourceTab = lWorkItem.LWorkRelaySource;
        if (pStripSourceTab == Guid.Empty)
        {
            return null;
        }

        if (LCartographerPlanStore.LCartographerPlanRead(lWorkItem.LWorkBatchId, out LCartographerPlanRecord pStripPlan)
            && pStripPlan.LCartographerStages.FirstOrDefault(
                pStripStage => pStripStage.LCartographerStageId == pStripSourceTab) is { } pStripSourceStage)
        {
            pStripSourceTab = pStripSourceStage.LCartographerOriginalTab;
        }

        return PStripTabFind(pStripSourceTab);
    }

    public static PTabRecord? PStripTabFind(Guid pStripTabId) =>
        PStripCurrent?.PStripRecords.FirstOrDefault(pTabRecord => pTabRecord.PTabId == pStripTabId);

    public void PStripNameSet(PTabRecord pTabRecord, string pTabName)
    {
        string pTabTrimmed = (pTabName ?? string.Empty).Trim();
        if (pTabTrimmed.Length == 0
            || string.Equals(pTabTrimmed, PStripTitleRead(pTabRecord.PTabLayoutKey), StringComparison.Ordinal))
        {
            if (pTabRecord.PTabNameCustom.Length > 0)
            {
                LTraceLog.LTraceInfoRecord($"Tab name reset to the standard name for {pTabRecord.PTabLayoutKey}");
            }

            pTabRecord.PTabNameCustom = string.Empty;
            PStripTitleUpdate();
            return;
        }

        var pStripTaken = PStripRecords
            .Where(pTabItem => !ReferenceEquals(pTabItem, pTabRecord))
            .Select(pTabItem => pTabItem.PTabTitle)
            .ToList();
        pTabRecord.PTabNameCustom = LTabset.LTabsetNameDedup(
            pStripTaken,
            pTabTrimmed,
            (pStripName, pStripAttempt) => LLocalization.LLocalizationFormat("Tab.Numbered", pStripName, pStripAttempt));
        PStripTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab renamed to '{pTabRecord.PTabTitle}' ({pTabRecord.PTabLayoutKey})");
    }

    private static string PStripTitleRead(string pTabLayoutKey) =>
        LLocalization.LLocalizationTextRead(pTabLayoutKey switch
        {
            "Edit" => "Tab.Edit",
            "Audio" => "Tab.Audio",
            "Convert" => "Tab.Convert",
            "Merge" => "Tab.Merge",
            "Funnel" => "Tab.Funnel",
            "Worklist" => "Tab.Worklist",
            _ => "Tab.Split"
        });

    public void PStripSelect(PTabRecord? pTabRecord)
    {
        foreach (var pTabItem in PStripRecords)
        {
            pTabItem.PTabSelectState = ReferenceEquals(pTabItem, pTabRecord);
        }

        PStripSelected = pTabRecord;
        PStripSeparatorUpdate();
    }

    public void PStripMove(PTabRecord pTabRecord, int pTabTargetIndex)
    {
        int pTabSourceIndex = PStripRecords.IndexOf(pTabRecord);
        if (pTabSourceIndex < 0)
        {
            return;
        }

        int pTabClampedTargetIndex = Math.Clamp(pTabTargetIndex, 0, PStripRecords.Count - 1);
        if (pTabSourceIndex == pTabClampedTargetIndex)
        {
            return;
        }

        PStripRecords.Move(pTabSourceIndex, pTabClampedTargetIndex);
        PStripSeparatorUpdate();
    }

    public bool PStripContentClear()
    {
        bool pStripCleared = false;
        foreach (PTabRecord pTabRecord in PStripRecords)
        {
            pStripCleared |= pTabRecord.PTabWorkspace.PWorkspaceMediaClear();
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {PStripRecords.Count} tab(s)");
        return pStripCleared;
    }

    public void PStripClose(PTabRecord pTabRecord)
    {
        var pTabIndex = PStripRecords.IndexOf(pTabRecord);
        if (pTabIndex < 0)
        {
            return;
        }

        var pTabWasSelected = ReferenceEquals(PStripSelected, pTabRecord);
        if (ReferenceEquals(pStripHovered, pTabRecord))
        {
            pStripHovered = null;
        }

        string pTabClosedTitle = pTabRecord.PTabTitle;
        pTabRecord.PTabWorkspace.PWorkspaceClose();
        LCartographer.LCartographerTabRemove(pTabRecord.PTabId);
        PStripRecords.RemoveAt(pTabIndex);
        PStripTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab closed '{pTabClosedTitle}': {PStripRecords.Count} tab(s) open");

        if (!pTabWasSelected)
        {
            PStripSeparatorUpdate();
            return;
        }

        if (PStripRecords.Count == 0)
        {
            PStripSelected = null;
            return;
        }

        var pTabNextIndex = LTabset.LTabsetNextResolve(PStripRecords.Count, pTabIndex);
        PStripSelect(PStripRecords[pTabNextIndex]);
    }

    public void PStripAllClose()
    {
        foreach (PTabRecord pTabRecord in PStripRecords)
        {
            pTabRecord.PTabWorkspace.PWorkspaceClose();
            LCartographer.LCartographerTabRemove(pTabRecord.PTabId);
        }

        pStripHovered = null;
        PStripRecords.Clear();
        PStripSelected = null;
        LTraceLog.LTraceInfoRecord("All tabs closed");
    }
}
