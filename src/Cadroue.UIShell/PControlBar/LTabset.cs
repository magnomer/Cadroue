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
    private const string lTabsetWorklistIconPath = "/PAssets/PCompass/PActionAddList.png";

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

    /// <param name="lExportSpecificState">
    /// Export settings to restore into the new tab, or null to start from defaults.
    /// </param>
    public PTabRecord LTabsetAdd(string pTabLayoutKey, LExportSpecificState? lExportSpecificState = null)
    {
        return pTabLayoutKey switch
        {
            "Edit" => LTabsetTypedAdd("Edit", lTabsetEditIconPath, lExportSpecificState),
            "Audio" => LTabsetTypedAdd("Audio", lTabsetAudioIconPath, lExportSpecificState),
            "Convert" => LTabsetTypedAdd("Convert", lTabsetConvertIconPath, lExportSpecificState),
            "Merge" => LTabsetTypedAdd("Merge", lTabsetMergeIconPath, lExportSpecificState),
            "Worklist" => LTabsetTypedAdd("Worklist", lTabsetWorklistIconPath, lExportSpecificState),
            _ => LTabsetTypedAdd("Split", lTabsetSplitIconPath, lExportSpecificState)
        };
    }

    private PTabRecord LTabsetTypedAdd(
        string pTabLayoutKey,
        string pTabIconPath,
        LExportSpecificState? lExportSpecificState)
    {
        var pTabRecord = new PTabRecord(
            pTabLayoutKey,
            pTabLayoutKey,
            PIcon.PIconRead(pTabIconPath),
            lExportSpecificState)
        {
            PTabOrdinal = LTabsetOrdinalRead(pTabLayoutKey)
        };
        PTabsetRecords.Add(pTabRecord);
        LTabsetTitleUpdate();

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

    /// <summary>
    /// Lowest ordinal not currently taken by a tab of the same kind, so a number freed
    /// by a close is reused instead of counting upward forever.
    /// </summary>
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

    /// <summary>
    /// Rewrite every tab title from its own ordinal. A layout key present once is shown
    /// bare ("Split") and its ordinal resets to 1; repeats keep the number they were
    /// given at creation, so reordering swaps positions without swapping numbers.
    /// </summary>
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

        // No title update here: ordinals belong to the tab, not to the slot.
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
        pTabRecord.PTabWorkspace.PWorkspaceClose();
        PTabsetRecords.RemoveAt(pTabIndex);
        LTabsetTitleUpdate();

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
