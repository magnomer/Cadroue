using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class LSKeymapChord
{
    public LSKeymapChord(string lToken, string lGesture)
    {
        LSKeymapChordToken = lToken;
        LSKeymapChordGesture = lGesture;
    }

    public string LSKeymapChordToken { get; }

    public string LSKeymapChordGesture { get; internal set; }

    public string LSKeymapChordPending { get; internal set; } = string.Empty;

    public bool LSKeymapChordActive { get; internal set; }
}

public sealed class LSKeymap
{
    private readonly Dictionary<string, LSKeymapChord> lsKeymapChords = new(StringComparer.Ordinal);

    public event Action<LSKeymapChord>? LSKeymapChordChange;

    public LSKeymap()
        : this(LBinding.LBindingCurrent)
    {
    }

    public LSKeymap(List<LBindingRecord>? lRecords)
    {
        List<LBindingRecord> lDraft = LBinding.LBindingNormalize(lRecords);
        foreach (LBindingCommand lCommand in LBinding.LBindingCatalogRead())
        {
            string lToken = lCommand.LBindingCommandToken;
            lsKeymapChords[lToken] = new LSKeymapChord(lToken, LBinding.LBindingGestureRead(lDraft, lToken));
        }
    }

    public IReadOnlyCollection<LSKeymapChord> LSKeymapChords => lsKeymapChords.Values;

    public LSKeymapChord LSKeymapChordRead(string lToken) => lsKeymapChords[lToken];

    public void LSKeymapChordSet(LSKeymapChord lChord, string lGesture)
    {
        lChord.LSKeymapChordGesture = lGesture;
        lChord.LSKeymapChordPending = string.Empty;
        lChord.LSKeymapChordActive = false;
        LSKeymapChordChange?.Invoke(lChord);
    }

    public void LSKeymapChordStart(LSKeymapChord lChord)
    {
        lChord.LSKeymapChordActive = true;
        lChord.LSKeymapChordPending = string.Empty;
        LSKeymapChordChange?.Invoke(lChord);
    }

    public void LSKeymapPendingSet(LSKeymapChord lChord, string lGesture)
    {
        if (!lChord.LSKeymapChordActive || lGesture.Length == 0)
        {
            return;
        }

        lChord.LSKeymapChordPending = lGesture;
        LSKeymapChordChange?.Invoke(lChord);
    }

    public void LSKeymapChordCancel(LSKeymapChord lChord)
    {
        lChord.LSKeymapChordActive = false;
        lChord.LSKeymapChordPending = string.Empty;
        LSKeymapChordChange?.Invoke(lChord);
    }

    public void LSKeymapChordCommit(LSKeymapChord lChord)
    {
        lChord.LSKeymapChordActive = false;
        string lPending = lChord.LSKeymapChordPending;
        if (lPending.Length == 0)
        {
            LSKeymapChordChange?.Invoke(lChord);
            return;
        }

        lChord.LSKeymapChordGesture = lPending;
        lChord.LSKeymapChordPending = string.Empty;
        LSKeymapChordChange?.Invoke(lChord);
        foreach (LSKeymapChord lOther in lsKeymapChords.Values)
        {
            if (!ReferenceEquals(lOther, lChord)
                && string.Equals(lOther.LSKeymapChordGesture, lPending, StringComparison.OrdinalIgnoreCase))
            {
                LSKeymapChordSet(lOther, string.Empty);
            }
        }
    }

    public void LSKeymapDefaultApply()
    {
        foreach (LSKeymapChord lChord in lsKeymapChords.Values)
        {
            LSKeymapChordSet(lChord, LBinding.LBindingDefaultRead(lChord.LSKeymapChordToken));
        }
    }

    public List<LBindingRecord> LSKeymapRecordsRead() =>
        lsKeymapChords.Values
            .Select(lChord => new LBindingRecord
            {
                LBindingRecordToken = lChord.LSKeymapChordToken,
                LBindingRecordGesture = lChord.LSKeymapChordGesture
            })
            .ToList();

    public void LSKeymapApply() => LBinding.LBindingSet(LSKeymapRecordsRead());
}
