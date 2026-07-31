using System.Collections.ObjectModel;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PControlBar;

public sealed class LTabset
{
    private const string lTabsetSplitIcon = "/PAssets/PTabs/PSplitButton.svg";
    private const string lTabsetEditIcon = "/PAssets/PTabs/PEditButton.svg";
    private const string lTabsetAudioIcon = "/PAssets/PTabs/PAudioButton.svg";
    private const string lTabsetConvertIcon = "/PAssets/PTabs/PConvertButton.svg";
    private const string lTabsetMergeIcon = "/PAssets/PTabs/PMergeButton.svg";
    private const string lTabsetWorklistIcon = "/PAssets/PTabs/PWorklistButton.svg";

    private PTabRecord? pTabsetCurrent;

    public LTabset()
    {
        PTabsetRecords = new ObservableCollection<PTabRecord>();
        LTabsetCurrent = this;
    }

    public static LTabset? LTabsetCurrent { get; private set; }

    public ObservableCollection<PTabRecord> PTabsetRecords { get; }

    public PTabRecord? PTabsetCurrent
    {
        get => pTabsetCurrent;
        private set
        {
            if (ReferenceEquals(pTabsetCurrent, value))
            {
                return;
            }

            pTabsetCurrent = value;
            LTabsetSelectChange?.Invoke(pTabsetCurrent);
        }
    }

    public event Action<PTabRecord?>? LTabsetSelectChange;

    private void LTabsetSeparatorUpdate()
    {
        var selectedIndex = PTabsetCurrent is null ? -1 : PTabsetRecords.IndexOf(PTabsetCurrent);
        for (var i = 0; i < PTabsetRecords.Count; i++)
        {
            PTabsetRecords[i].PTabSeparatorState =
                i < PTabsetRecords.Count - 1
                && i != selectedIndex
                && i != selectedIndex - 1;
        }
    }

    public PTabRecord LTabsetAdd()
    {
        return LTabsetAdd("Split");
    }

    public PTabRecord LTabsetAdd(
        string pTabLayoutKey,
        LPreset? lExportSpecificState = null,
        LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        return pTabLayoutKey switch
        {
            "Edit" => LTabsetTypedAdd("Edit", lTabsetEditIcon, lExportSpecificState, lPreferenceTabLayout),
            "Audio" => LTabsetTypedAdd("Audio", lTabsetAudioIcon, lExportSpecificState, lPreferenceTabLayout),
            "Convert" => LTabsetTypedAdd("Convert", lTabsetConvertIcon, lExportSpecificState, lPreferenceTabLayout),
            "Merge" => LTabsetTypedAdd("Merge", lTabsetMergeIcon, lExportSpecificState, lPreferenceTabLayout),
            "Worklist" => LTabsetTypedAdd("Worklist", lTabsetWorklistIcon, lExportSpecificState, lPreferenceTabLayout),
            _ => LTabsetTypedAdd("Split", lTabsetSplitIcon, lExportSpecificState, lPreferenceTabLayout)
        };
    }

    private PTabRecord LTabsetTypedAdd(
        string pTabLayoutKey,
        string pTabIconPath,
        LPreset? lExportSpecificState,
        LPreferenceTabLayoutRecord? lPreferenceTabLayout)
    {
        var pTabRecord = new PTabRecord(
            LTabsetTitleRead(pTabLayoutKey),
            pTabLayoutKey,
            PIcon.PIconRead(pTabIconPath),
            lExportSpecificState,
            lPreferenceTabLayout)
        {
            PTabOrdinal = LTabsetOrdinalRead(pTabLayoutKey)
        };
        PTabsetRecords.Add(pTabRecord);
        LTabsetTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab opened '{pTabRecord.PTabTitle}' ({pTabLayoutKey}): {PTabsetRecords.Count} tab(s) open");

        if (PTabsetCurrent is null)
        {
            LTabsetSelect(pTabRecord);
        }
        else
        {
            LTabsetSeparatorUpdate();
        }

        return pTabRecord;
    }

    private int LTabsetOrdinalRead(string pTabLayoutKey)
    {
        var lTabsetTakenOrdinals = PTabsetRecords
            .Where(pTabItem => string.Equals(pTabItem.PTabLayoutKey, pTabLayoutKey, StringComparison.Ordinal))
            .Select(pTabItem => pTabItem.PTabOrdinal)
            .ToHashSet();

        int pTabOrdinal = 1;
        while (lTabsetTakenOrdinals.Contains(pTabOrdinal))
        {
            pTabOrdinal++;
        }

        return pTabOrdinal;
    }

    private void LTabsetTitleUpdate()
    {
        foreach (PTabRecord pTabItem in PTabsetRecords)
        {
            if (pTabItem.PTabNameCustom.Length > 0)
            {
                pTabItem.PTabTitle = pTabItem.PTabNameCustom;
            }
        }

        foreach (var lTabsetKindGroup in PTabsetRecords
                     .Where(pTabItem => pTabItem.PTabNameCustom.Length == 0)
                     .GroupBy(pTabItem => pTabItem.PTabLayoutKey, StringComparer.Ordinal))
        {
            var lTabsetKindTabs = lTabsetKindGroup.ToList();
            if (lTabsetKindTabs.Count == 1)
            {
                lTabsetKindTabs[0].PTabOrdinal = 1;
                lTabsetKindTabs[0].PTabTitle = LTabsetTitleRead(lTabsetKindGroup.Key);
                continue;
            }

            foreach (PTabRecord pTabItem in lTabsetKindTabs)
            {
                pTabItem.PTabTitle = LLocalization.LLocalizationFormat(
                    "Tab.Numbered",
                    LTabsetTitleRead(lTabsetKindGroup.Key),
                    pTabItem.PTabOrdinal);
            }
        }

        PMainArea.LCourier.LCourierFaceUpdate();
    }

    public void LTabsetNameSet(PTabRecord pTabRecord, string pTabName)
    {
        string pTabTrimmed = (pTabName ?? string.Empty).Trim();
        if (pTabTrimmed.Length == 0
            || string.Equals(pTabTrimmed, LTabsetTitleRead(pTabRecord.PTabLayoutKey), StringComparison.Ordinal))
        {
            if (pTabRecord.PTabNameCustom.Length > 0)
            {
                LTraceLog.LTraceInfoRecord($"Tab name reset to the standard name for {pTabRecord.PTabLayoutKey}");
            }

            pTabRecord.PTabNameCustom = string.Empty;
            LTabsetTitleUpdate();
            return;
        }

        pTabRecord.PTabNameCustom = LTabsetNameResolve(pTabRecord, pTabTrimmed);
        LTabsetTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab renamed to '{pTabRecord.PTabTitle}' ({pTabRecord.PTabLayoutKey})");
    }

    private string LTabsetNameResolve(PTabRecord pTabRecord, string pTabName)
    {
        var lTabsetTakenNames = PTabsetRecords
            .Where(pTabItem => !ReferenceEquals(pTabItem, pTabRecord))
            .Select(pTabItem => pTabItem.PTabTitle)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string pTabDistinct = pTabName;
        int pTabAttempt = 2;
        while (lTabsetTakenNames.Contains(pTabDistinct))
        {
            pTabDistinct = LLocalization.LLocalizationFormat("Tab.Numbered", pTabName, pTabAttempt);
            pTabAttempt++;
        }

        return pTabDistinct;
    }

    private static string LTabsetTitleRead(string pTabLayoutKey) =>
        LLocalization.LLocalizationTextRead(pTabLayoutKey switch
        {
            "Edit" => "Tab.Edit",
            "Audio" => "Tab.Audio",
            "Convert" => "Tab.Convert",
            "Merge" => "Tab.Merge",
            "Worklist" => "Tab.Worklist",
            _ => "Tab.Split"
        });

    public void LTabsetSelect(PTabRecord? pTabRecord)
    {
        foreach (var pTabItem in PTabsetRecords)
        {
            pTabItem.PTabSelectState = ReferenceEquals(pTabItem, pTabRecord);
        }

        PTabsetCurrent = pTabRecord;
        LTabsetSeparatorUpdate();
    }

    public void LTabsetMove(PTabRecord pTabRecord, int pTabTargetIndex)
    {
        int pTabSourceIndex = PTabsetRecords.IndexOf(pTabRecord);
        if (pTabSourceIndex < 0)
        {
            return;
        }

        int pTabClampedTargetIndex = Math.Clamp(pTabTargetIndex, 0, PTabsetRecords.Count - 1);
        if (pTabSourceIndex == pTabClampedTargetIndex)
        {
            return;
        }

        PTabsetRecords.Move(pTabSourceIndex, pTabClampedTargetIndex);
        LTabsetSeparatorUpdate();
    }

    public bool LTabsetContentClear()
    {
        bool pTabsetCleared = false;
        foreach (PTabRecord pTabRecord in PTabsetRecords)
        {
            PWorkspace pTabWorkspace = pTabRecord.PTabWorkspace;
            pTabsetCleared |= pTabWorkspace.PWorkspaceViewer?.PViewerMediaClose(true) == true;

            if (pTabWorkspace.PWorkspaceList is { } pTabList && pTabList.PListPathsRead().Count > 0)
            {
                pTabList.PListClear();
                pTabsetCleared = true;
            }

            if (pTabWorkspace.PWorkspaceSurface.PTabGroup is { } pTabGroup)
            {
                pTabGroup.PGroupClear();
            }
        }

        LTraceLog.LTraceInfoRecord($"Tabs cleared across {PTabsetRecords.Count} tab(s)");
        return pTabsetCleared;
    }

    public void LTabsetClose(PTabRecord pTabRecord)
    {
        var pTabIndex = PTabsetRecords.IndexOf(pTabRecord);
        if (pTabIndex < 0)
        {
            return;
        }

        var pTabWasSelected = ReferenceEquals(PTabsetCurrent, pTabRecord);
        string pTabClosedTitle = pTabRecord.PTabTitle;
        pTabRecord.PTabWorkspace.PWorkspaceClose();
        PMainArea.LCourier.LCourierTabRemove(pTabRecord.PTabId);
        PTabsetRecords.RemoveAt(pTabIndex);
        LTabsetTitleUpdate();
        LTraceLog.LTraceInfoRecord($"Tab closed '{pTabClosedTitle}': {PTabsetRecords.Count} tab(s) open");

        if (!pTabWasSelected)
        {
            LTabsetSeparatorUpdate();
            return;
        }

        if (PTabsetRecords.Count == 0)
        {
            PTabsetCurrent = null;
            return;
        }

        var pTabNextIndex = Math.Min(pTabIndex, PTabsetRecords.Count - 1);
        LTabsetSelect(PTabsetRecords[pTabNextIndex]);
    }
}
