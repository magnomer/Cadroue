using System.Collections.ObjectModel;

namespace Cadroue.UIShell.PControlBar;

public sealed class LTabSelect
{
    private const string lTabSplitIconPath = "/PAssets/PTabs/PSplitButton.png";
    private const string lTabEditIconPath = "/PAssets/PTabs/PEditButton.png";
    private const string lTabAudioIconPath = "/PAssets/PTabs/PAudioButton.png";
    private const string lTabConvertIconPath = "/PAssets/PTabs/PConvertButton.png";
    private const string lTabMergeIconPath = "/PAssets/PTabs/PMergeButton.png";
    private const string lTabWorklistIconPath = "/PAssets/PCompass/PActionAddList.png";

    private int lTabCreateIndex;
    private PTabRecord? pTabSelectRecord;

    public LTabSelect()
    {
        PTabRecords = new ObservableCollection<PTabRecord>();
    }

    public ObservableCollection<PTabRecord> PTabRecords { get; }

    public PTabRecord? PTabSelectRecord
    {
        get => pTabSelectRecord;
        private set
        {
            if (ReferenceEquals(pTabSelectRecord, value))
            {
                return;
            }

            pTabSelectRecord = value;
            LTabSelectChange?.Invoke(pTabSelectRecord);
        }
    }

    public event Action<PTabRecord?>? LTabSelectChange;

    private void LTabUpdateSeparators()
    {
        var selectedIndex = PTabSelectRecord is null ? -1 : PTabRecords.IndexOf(PTabSelectRecord);
        for (var i = 0; i < PTabRecords.Count; i++)
        {
            PTabRecords[i].PTabSeparatorState =
                i < PTabRecords.Count - 1
                && i != selectedIndex
                && i != selectedIndex - 1;
        }
    }

    public PTabRecord LTabAddRequest()
    {
        return LTabAddRequest("Split");
    }

    public PTabRecord LTabAddRequest(string pTabLayoutKey)
    {
        return pTabLayoutKey switch
        {
            "Edit" => LTabAddTypedRequest("Edit", lTabEditIconPath),
            "Audio" => LTabAddTypedRequest("Audio", lTabAudioIconPath),
            "Convert" => LTabAddTypedRequest("Convert", lTabConvertIconPath),
            "Merge" => LTabAddTypedRequest("Merge", lTabMergeIconPath),
            "Worklist" => LTabAddTypedRequest("Worklist", lTabWorklistIconPath),
            _ => LTabAddTypedRequest("Split", lTabSplitIconPath)
        };
    }

    public PTabRecord LTabAddRequest(string pTabTitle, string pTabLayoutKey, string pTabIconPath)
    {
        var pTabRecord = new PTabRecord(pTabTitle, pTabLayoutKey, pTabIconPath);
        PTabRecords.Add(pTabRecord);

        if (PTabSelectRecord is null)
        {
            LTabSelectRequest(pTabRecord);
        }
        else
        {
            LTabUpdateSeparators();
        }

        return pTabRecord;
    }

    private PTabRecord LTabAddTypedRequest(string pTabLayoutKey, string pTabIconPath)
    {
        lTabCreateIndex++;
        return LTabAddRequest($"{pTabLayoutKey} {lTabCreateIndex}", pTabLayoutKey, pTabIconPath);
    }

    public void LTabSelectRequest(PTabRecord? pTabRecord)
    {
        foreach (var pTabItem in PTabRecords)
        {
            pTabItem.PTabSelectState = ReferenceEquals(pTabItem, pTabRecord);
        }

        PTabSelectRecord = pTabRecord;
        LTabUpdateSeparators();
    }

    public void LTabMoveRequest(PTabRecord pTabRecord, int pTabTargetIndex)
    {
        int pTabSourceIndex = PTabRecords.IndexOf(pTabRecord);
        if (pTabSourceIndex < 0)
        {
            return;
        }

        int pTabClampedTargetIndex = Math.Clamp(pTabTargetIndex, 0, PTabRecords.Count - 1);
        if (pTabSourceIndex == pTabClampedTargetIndex)
        {
            return;
        }

        PTabRecords.Move(pTabSourceIndex, pTabClampedTargetIndex);
        LTabUpdateSeparators();
    }

    public void LTabCloseRequest(PTabRecord pTabRecord)
    {
        var pTabIndex = PTabRecords.IndexOf(pTabRecord);
        if (pTabIndex < 0)
        {
            return;
        }

        var pTabWasSelected = ReferenceEquals(PTabSelectRecord, pTabRecord);
        pTabRecord.PTabWorkspace.PTabWorkspaceCloseRequest();
        PTabRecords.RemoveAt(pTabIndex);

        if (!pTabWasSelected)
        {
            LTabUpdateSeparators();
            return;
        }

        if (PTabRecords.Count == 0)
        {
            PTabSelectRecord = null;
            return;
        }

        var pTabNextIndex = Math.Min(pTabIndex, PTabRecords.Count - 1);
        LTabSelectRequest(PTabRecords[pTabNextIndex]);
    }
}
