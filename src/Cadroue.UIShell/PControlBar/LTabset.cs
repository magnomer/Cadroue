using System.Collections.ObjectModel;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PPanels;

namespace Cadroue.UIShell.PControlBar;

public sealed class LTabset
{
    private const string lTabsetSplitIconPath = "/PAssets/PTabs/PSplitButton.svg";
    private const string lTabsetEditIconPath = "/PAssets/PTabs/PEditButton.png";
    private const string lTabsetAudioIconPath = "/PAssets/PTabs/PAudioButton.png";
    private const string lTabsetConvertIconPath = "/PAssets/PTabs/PConvertButton.png";
    private const string lTabsetMergeIconPath = "/PAssets/PTabs/PMergeButton.png";
    private const string lTabsetWorklistIconPath = "/PAssets/PTabs/PWorklistButton.svg";

    private PTabRecord? pTabsetSelectRecord;

    public LTabset()
    {
        PTabsetRecords = new ObservableCollection<PTabRecord>();
    }

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
            pTabLayoutKey,
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
        foreach (var lTabsetKindGroup in PTabsetRecords.GroupBy(
                     pTabItem => pTabItem.PTabLayoutKey, StringComparer.Ordinal))
        {
            var lTabsetKindTabs = lTabsetKindGroup.ToList();
            if (lTabsetKindTabs.Count == 1)
            {
                lTabsetKindTabs[0].PTabOrdinal = 1;
                lTabsetKindTabs[0].PTabTitle = lTabsetKindGroup.Key;
                continue;
            }

            foreach (PTabRecord pTabItem in lTabsetKindTabs)
            {
                pTabItem.PTabTitle = $"{lTabsetKindGroup.Key} ({pTabItem.PTabOrdinal})";
            }
        }
    }

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
