using System.Collections.ObjectModel;

namespace Cadroue.UIShell.PControlBar;

public sealed class LTabset
{
    private const string lTabsetSplitIconPath = "/PAssets/PTabs/PSplitButton.png";
    private const string lTabsetEditIconPath = "/PAssets/PTabs/PEditButton.png";
    private const string lTabsetAudioIconPath = "/PAssets/PTabs/PAudioButton.png";
    private const string lTabsetConvertIconPath = "/PAssets/PTabs/PConvertButton.png";
    private const string lTabsetMergeIconPath = "/PAssets/PTabs/PMergeButton.png";
    private const string lTabsetWorklistIconPath = "/PAssets/PCompass/PActionAddList.png";

    private int lTabsetCreateIndex;
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

    public PTabRecord LTabsetAdd(string pTabLayoutKey)
    {
        return pTabLayoutKey switch
        {
            "Edit" => LTabsetTypedAdd("Edit", lTabsetEditIconPath),
            "Audio" => LTabsetTypedAdd("Audio", lTabsetAudioIconPath),
            "Convert" => LTabsetTypedAdd("Convert", lTabsetConvertIconPath),
            "Merge" => LTabsetTypedAdd("Merge", lTabsetMergeIconPath),
            "Worklist" => LTabsetTypedAdd("Worklist", lTabsetWorklistIconPath),
            _ => LTabsetTypedAdd("Split", lTabsetSplitIconPath)
        };
    }

    public PTabRecord LTabsetAdd(string pTabTitle, string pTabLayoutKey, string pTabIconPath)
    {
        var pTabRecord = new PTabRecord(pTabTitle, pTabLayoutKey, pTabIconPath);
        PTabsetRecords.Add(pTabRecord);

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

    private PTabRecord LTabsetTypedAdd(string pTabLayoutKey, string pTabIconPath)
    {
        lTabsetCreateIndex++;
        return LTabsetAdd($"{pTabLayoutKey} {lTabsetCreateIndex}", pTabLayoutKey, pTabIconPath);
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
        pTabRecord.PTabWorkspace.PWorkspaceClose();
        PTabsetRecords.RemoveAt(pTabIndex);

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
