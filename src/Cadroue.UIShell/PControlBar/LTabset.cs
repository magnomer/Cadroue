using System.Collections.ObjectModel;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PControlBar;

public sealed class LTabset
{
    private const string lTabsetSplitIconPath = "/PAssets/PTabs/PSplitButton.svg";
    private const string lTabsetEditIconPath = "/PAssets/PTabs/PEditButton.svg";
    private const string lTabsetAudioIconPath = "/PAssets/PTabs/PAudioButton.svg";
    private const string lTabsetConvertIconPath = "/PAssets/PTabs/PConvertButton.svg";
    private const string lTabsetMergeIconPath = "/PAssets/PTabs/PMergeButton.svg";
    private const string lTabsetWorklistIconPath = "/PAssets/PTabs/PWorklistButton.svg";

    private PTabRecord? pTabsetSelectRecord;

    public LTabset()
    {
        PTabsetRecords = new ObservableCollection<PTabRecord>();
        LTabsetCurrent = this;
    }

    public static LTabset? LTabsetCurrent { get; private set; }

    public ObservableCollection<PTabRecord> PTabsetRecords { get; }

    public PTabRecord? PTabsetSelectRecord
    {
        get => pTabsetSelectRecord;
        private set
        {
            if (ReferenceEquals(pTabsetSelectRecord, value))
            {
                return;
            }

            pTabsetSelectRecord = value;
            LTabsetSelectChange?.Invoke(pTabsetSelectRecord);
        }
    }

    public event Action<PTabRecord?>? LTabsetSelectChange;

    private void LTabsetSeparatorUpdate()
    {
        var selectedIndex = PTabsetSelectRecord is null ? -1 : PTabsetRecords.IndexOf(PTabsetSelectRecord);
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
        LExportSpecificState? lExportSpecificState = null,
        LPreferenceTabLayoutRecord? lPreferenceTabLayout = null)
    {
        return pTabLayoutKey switch
        {
            "Edit" => LTabsetTypedAdd("Edit", lTabsetEditIconPath, lExportSpecificState, lPreferenceTabLayout),
            "Audio" => LTabsetTypedAdd("Audio", lTabsetAudioIconPath, lExportSpecificState, lPreferenceTabLayout),
            "Convert" => LTabsetTypedAdd("Convert", lTabsetConvertIconPath, lExportSpecificState, lPreferenceTabLayout),
            "Merge" => LTabsetTypedAdd("Merge", lTabsetMergeIconPath, lExportSpecificState, lPreferenceTabLayout),
            "Worklist" => LTabsetTypedAdd("Worklist", lTabsetWorklistIconPath, lExportSpecificState, lPreferenceTabLayout),
            _ => LTabsetTypedAdd("Split", lTabsetSplitIconPath, lExportSpecificState, lPreferenceTabLayout)
        };
    }

    private PTabRecord LTabsetTypedAdd(
        string pTabLayoutKey,
        string pTabIconPath,
        LExportSpecificState? lExportSpecificState,
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
        LAppLog.LInfo($"Tab opened '{pTabRecord.PTabTitle}' ({pTabLayoutKey}): {PTabsetRecords.Count} tab(s) open");

        if (PTabsetSelectRecord is null)
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
                LAppLog.LInfo($"Tab name reset to the standard name for {pTabRecord.PTabLayoutKey}");
            }

            pTabRecord.PTabNameCustom = string.Empty;
            LTabsetTitleUpdate();
            return;
        }

        pTabRecord.PTabNameCustom = LTabsetNameDistinct(pTabRecord, pTabTrimmed);
        LTabsetTitleUpdate();
        LAppLog.LInfo($"Tab renamed to '{pTabRecord.PTabTitle}' ({pTabRecord.PTabLayoutKey})");
    }

    private string LTabsetNameDistinct(PTabRecord pTabRecord, string pTabName)
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

        PTabsetSelectRecord = pTabRecord;
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

    public void LTabsetClose(PTabRecord pTabRecord)
    {
        var pTabIndex = PTabsetRecords.IndexOf(pTabRecord);
        if (pTabIndex < 0)
        {
            return;
        }

        var pTabWasSelected = ReferenceEquals(PTabsetSelectRecord, pTabRecord);
        string pTabClosedTitle = pTabRecord.PTabTitle;
        pTabRecord.PTabWorkspace.PWorkspaceClose();
        PMainArea.LCourier.LCourierTabRemove(pTabRecord.PTabId);
        PTabsetRecords.RemoveAt(pTabIndex);
        LTabsetTitleUpdate();
        LAppLog.LInfo($"Tab closed '{pTabClosedTitle}': {PTabsetRecords.Count} tab(s) open");

        if (!pTabWasSelected)
        {
            LTabsetSeparatorUpdate();
            return;
        }

        if (PTabsetRecords.Count == 0)
        {
            PTabsetSelectRecord = null;
            return;
        }

        var pTabNextIndex = Math.Min(pTabIndex, PTabsetRecords.Count - 1);
        LTabsetSelect(PTabsetRecords[pTabNextIndex]);
    }
}
